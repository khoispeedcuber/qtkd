namespace QTKD.Api.Exceptions;

/// <summary>
/// Thao tác xung đột với trạng thái hiện tại của dữ liệu (sai trạng thái, trùng khóa).
/// Middleware chuyển thành HTTP 409.
/// </summary>
public class ConflictException : Exception
{
    public string? Rule { get; }

    public ConflictException(string message, string? rule = null) : base(message) => Rule = rule;
}
