using WalletSystem.Demo.Client.Core;
using WalletSystem.Application.Contracts.Services.Id;
using WalletSystem.Demo.Shared.Dtos;

namespace WalletSystem.Demo.Client.Observers;

/// <summary>
/// Console observer that prints test results to the console
/// Applied Observer Pattern
/// </summary>
public sealed class ConsoleTestObserver : ITestObserver
{
    public void OnTestStarted(string testName)
    {
        Console.WriteLine($"  [{testName}] Starting...");
    }

    public void OnTestCompleted(TestResult result)
    {
        var status = result.Passed ? "✓ PASS" : "✗ FAIL";
        var color = result.Passed ? ConsoleColor.Green : ConsoleColor.Red;
        Console.ForegroundColor = color;
        Console.WriteLine($"  [{status}] {result.TestName}: {result.Message}");
        Console.ResetColor();
    }

    public void OnTestSuiteCompleted(TestSuiteResult suiteResult)
    {
        Console.WriteLine();
        Console.WriteLine($"=== {suiteResult.SuiteName} Suite Completed ===");
        Console.WriteLine($"Total: {suiteResult.TotalCount} | Passed: {suiteResult.PassedCount} | Failed: {suiteResult.FailedCount}");
        Console.WriteLine($"Summary: {suiteResult.SummaryMessage}");
        Console.WriteLine();
    }
}

/// <summary>
/// Aggregating observer that forwards events to multiple observers
/// Applied Composite Pattern with Observer Pattern
/// </summary>
public sealed class AggregatingTestObserver : ITestObserver
{
    private readonly IReadOnlyList<ITestObserver> _observers;

    public AggregatingTestObserver(params ITestObserver[] observers)
    {
        _observers = observers ?? [];
    }

    public void OnTestStarted(string testName)
    {
        foreach (var observer in _observers)
        {
            observer.OnTestStarted(testName);
        }
    }

    public void OnTestCompleted(TestResult result)
    {
        foreach (var observer in _observers)
        {
            observer.OnTestCompleted(result);
        }
    }

    public void OnTestSuiteCompleted(TestSuiteResult suiteResult)
    {
        foreach (var observer in _observers)
        {
            observer.OnTestSuiteCompleted(suiteResult);
        }
    }
}

/// <summary>
/// Null object observer - does nothing
/// Applied Null Object Pattern - provides default no-op behavior
/// </summary>
public sealed class NullTestObserver : ITestObserver
{
    public static readonly NullTestObserver Instance = new();

    private NullTestObserver() { }

    public void OnTestStarted(string testName) { }
    public void OnTestCompleted(TestResult result) { }
    public void OnTestSuiteCompleted(TestSuiteResult suiteResult) { }
}
