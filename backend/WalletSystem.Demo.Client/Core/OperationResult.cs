namespace WalletSystem.Demo.Client.Core;

/// <summary>
/// Result of a single operation execution
/// Applied Flyweight pattern for lightweight result objects
/// </summary>
public sealed record OperationResult(
    bool Success,
    string Message,
    string? Reference = null,
    int? HttpStatusCode = null,
    string? ResponseContent = null
);
