using WalletSystem.Demo.Shared.Dtos;

namespace WalletSystem.Demo.Client.Core;

/// <summary>
/// Aggregated results from a test suite
/// Applied Composite pattern to treat individual and aggregated results uniformly
/// </summary>
public sealed record TestSuiteResult(
    string SuiteName,
    bool Passed,
    IReadOnlyList<TestResult> Results,
    string SummaryMessage,
    IReadOnlyDictionary<string, string>? ContextData = null
)
{
    public int TotalCount => Results.Count;
    public int PassedCount => Results.Count(r => r.Passed);
    public int FailedCount => Results.Count(r => !r.Passed);
}
