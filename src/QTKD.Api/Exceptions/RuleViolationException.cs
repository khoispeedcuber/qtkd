namespace QTKD.Api.Exceptions;

/// <summary>
/// Vi phạm quy tắc nghiệp vụ (ví dụ CTR-02, CTR-03). Middleware chuyển thành HTTP 400,
/// kèm mã quy tắc (<see cref="Rule"/>) và danh sách lỗi theo trường (<see cref="Errors"/>).
/// </summary>
public class RuleViolationException : Exception
{
    public string Rule { get; }
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public RuleViolationException(string rule, string message,
        IReadOnlyDictionary<string, string[]>? errors = null) : base(message)
    {
        Rule = rule;
        Errors = errors ?? new Dictionary<string, string[]>();
    }
}
