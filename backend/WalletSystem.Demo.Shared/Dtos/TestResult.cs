namespace WalletSystem.Demo.Shared.Dtos;

/// <summary>
/// Represents the result of a test execution with detailed failure information
/// </summary>
public record TestResult(
    string TestName,
    bool Passed,
    string Message,
    string? ErrorCode = null,
    int? HttpStatusCode = null,
    string? ExpectedValue = null,
    string? ActualValue = null,
    ExceptionInfo? ExceptionDetails = null,
    DateTime Timestamp = default,
    TimeSpan? Duration = null,
    Dictionary<string, string>? ContextData = null
)
{
    public TestResult(string testName, bool passed, string message)
        : this(testName, passed, message, null, null, null, null, null, DateTime.UtcNow, null, null)
    {
    }

    public TestResult WithContext(string key, string value)
    {
        var context = ContextData ?? new Dictionary<string, string>();
        context[key] = value;
        return this with { ContextData = context };
    }
}
