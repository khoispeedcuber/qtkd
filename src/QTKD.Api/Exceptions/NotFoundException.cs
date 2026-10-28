namespace QTKD.Api.Exceptions;

/// <summary>Không tìm thấy dữ liệu được yêu cầu. Middleware chuyển thành HTTP 404.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
