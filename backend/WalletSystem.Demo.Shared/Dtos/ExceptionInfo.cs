namespace WalletSystem.Demo.Shared.Dtos;

/// <summary>
/// Captures exception details for debugging
/// </summary>
public record ExceptionInfo(
    string Type,
    string Message,
    string? StackTrace = null,
    ExceptionInfo? InnerException = null
)
{
    public static ExceptionInfo FromException(Exception ex)
    {
        return new ExceptionInfo(
            ex.GetType().Name,
            ex.Message,
            ex.StackTrace,
            ex.InnerException != null ? FromException(ex.InnerException) : null
        );
    }
}
