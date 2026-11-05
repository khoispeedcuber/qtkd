using Microsoft.AspNetCore.Mvc;
using QTKD.Api.Exceptions;

namespace QTKD.Api.Middleware;

/// <summary>
/// Bắt mọi exception chưa được xử lý và trả về một định dạng lỗi thống nhất (ProblemDetails),
/// thay vì try/catch lặp lại ở từng Controller.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client tự hủy request: không phải lỗi hệ thống.
            _logger.LogInformation("Client đã hủy request {Method} {Path}.",
                context.Request.Method, context.Request.Path);
            context.Response.StatusCode = 499;
        }
        catch (Exception ex)
        {
            await WriteProblemAsync(context, ex);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception ex)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogError(ex, "Có lỗi sau khi response đã bắt đầu gửi; không thể ghi lỗi chuẩn hóa.");
            return;
        }

        var (status, title, detail) = ex switch
        {
            NotFoundException =>
                (StatusCodes.Status404NotFound, "Không tìm thấy dữ liệu", ex.Message),
            RequestValidationException =>
                (StatusCodes.Status400BadRequest, "Dữ liệu yêu cầu không hợp lệ", ex.Message),
            RuleViolationException =>
                (StatusCodes.Status400BadRequest, "Vi phạm quy tắc nghiệp vụ", ex.Message),
            ConflictException =>
                (StatusCodes.Status409Conflict, "Xung đột trạng thái dữ liệu", ex.Message),
            _ =>
                (StatusCodes.Status500InternalServerError, "Lỗi hệ thống",
                 "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau.")
        };

        if (status >= StatusCodes.Status500InternalServerError)
            _logger.LogError(ex, "Lỗi không xử lý được tại {Method} {Path}.",
                context.Request.Method, context.Request.Path);
        else
            _logger.LogWarning("{Title}: {Detail}", title, detail);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;

        // Thông tin thêm cho lỗi nghiệp vụ: mã quy tắc và lỗi theo trường (cùng dạng "errors" với lỗi model validation).
        switch (ex)
        {
            case RuleViolationException rule:
                problem.Extensions["rule"] = rule.Rule;
                if (rule.Errors.Count > 0) problem.Extensions["errors"] = rule.Errors;
                break;
            case ConflictException { Rule: not null } conflict:
                problem.Extensions["rule"] = conflict.Rule;
                break;
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
    }
}
