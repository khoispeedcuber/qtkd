namespace QTKD.Api.Exceptions;

/// <summary>Dữ liệu đầu vào không hợp lệ. Middleware chuyển thành HTTP 400.</summary>
public class RequestValidationException : Exception
{
    public RequestValidationException(string message) : base(message) { }
}
