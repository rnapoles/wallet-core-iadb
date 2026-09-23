using WalletSystem.Demo.Client.Core;

namespace WalletSystem.Demo.Client.Facades;

/// <summary>
/// Summary of a complete test run
/// </summary>
public sealed record TestRunSummary(
    DateTime StartTime,
    DateTime EndTime,
    TimeSpan Duration,
    int TotalTests,
    int PassedTests,
    int FailedTests,
    bool AllPassed,
    IReadOnlyList<TestSuiteResult> SuiteResults
);

