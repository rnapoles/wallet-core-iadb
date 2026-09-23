using WalletSystem.Demo.Client.Core;
using WalletSystem.Demo.Client.Observers;
using WalletSystem.Demo.Client.Strategies;
using WalletSystem.Demo.Shared.Dtos;

namespace WalletSystem.Demo.Client.Facades;

/// <summary>
/// Facade for running wallet system tests
/// Applied Facade Pattern - provides simplified interface to complex testing subsystem
/// </summary>
public sealed class TestRunnerFacade
{
    private readonly HttpClient _httpClient;
    private readonly ITestObserver _observer;
    private readonly IReadOnlyList<ITestStrategy> _strategies;

    public TestRunnerFacade(
        HttpClient httpClient,
        ITestObserver? observer = null,
        IReadOnlyList<ITestStrategy>? strategies = null)
    {
        _httpClient = httpClient;
        _observer = observer ?? NullTestObserver.Instance;
        _strategies = strategies ?? Factories.TestStrategyFactory.CreateAll();
    }

    /// <summary>
    /// Run all test suites with the specified context
    /// </summary>
    public async Task<TestRunSummary> RunAllAsync(TestContext context, CancellationToken cancellationToken = default)
    {
        var suiteResults = new List<TestSuiteResult>();
        var startTime = DateTime.UtcNow;

        Console.WriteLine("===========================================");
        Console.WriteLine("WalletSystem Test Suite");
        Console.WriteLine("===========================================");
        Console.WriteLine($"Test started at: {startTime:yyyy-MM-dd HH:mm:ss.fff} UTC");
        Console.WriteLine($"User: {context.Email}");
        Console.WriteLine($"Wallet: {context.WalletId:D}");
        Console.WriteLine();

        foreach (var strategy in _strategies)
        {
            try
            {
                _observer.OnTestStarted($"{strategy.Name} Suite");
                
                Console.WriteLine($"--- Running {strategy.Name} Tests ---");
                var suiteResult = await strategy.ExecuteAsync(context, cancellationToken);
                suiteResults.Add(suiteResult);
                
                _observer.OnTestSuiteCompleted(suiteResult);
                
                // Print individual results
                foreach (var result in suiteResult.Results)
                {
                    _observer.OnTestCompleted(result);
                }
            }
            catch (Exception ex)
            {
                var errorResult = new TestSuiteResult(
                    strategy.Name,
                    false,
                    Array.Empty<TestResult>(),
                    $"Suite failed: {ex.Message}"
                );
                suiteResults.Add(errorResult);
                _observer.OnTestSuiteCompleted(errorResult);
            }

            // Reset authorization header between test suites
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }

        var endTime = DateTime.UtcNow;
        var summary = CreateSummary(suiteResults, startTime, endTime);
        
        PrintSummary(summary);
        
        return summary;
    }

    /// <summary>
    /// Run a single test strategy by name
    /// </summary>
    public async Task<TestSuiteResult> RunSingleAsync(string strategyName, TestContext context, CancellationToken cancellationToken = default)
    {
        var strategy = Factories.TestStrategyFactory.Create(strategyName);
        
        Console.WriteLine($"--- Running {strategy.Name} Tests ---");
        _observer.OnTestStarted($"{strategy.Name} Suite");
        
        var result = await strategy.ExecuteAsync(context, cancellationToken);
        
        _observer.OnTestSuiteCompleted(result);
        
        foreach (var r in result.Results)
        {
            _observer.OnTestCompleted(r);
        }

        return result;
    }

    private static TestRunSummary CreateSummary(IReadOnlyList<TestSuiteResult> suiteResults, DateTime startTime, DateTime endTime)
    {
        var allResults = suiteResults.SelectMany(s => s.Results).ToList();
        var totalTests = allResults.Count;
        var passedTests = allResults.Count(r => r.Passed);
        var failedTests = totalTests - passedTests;
        var allPassed = failedTests == 0;

        return new TestRunSummary(
            StartTime: startTime,
            EndTime: endTime,
            Duration: endTime - startTime,
            TotalTests: totalTests,
            PassedTests: passedTests,
            FailedTests: failedTests,
            AllPassed: allPassed,
            SuiteResults: suiteResults
        );
    }

    private static void PrintSummary(TestRunSummary summary)
    {
        Console.WriteLine();
        Console.WriteLine("===========================================");
        Console.WriteLine("TEST SUMMARY");
        Console.WriteLine("===========================================");
        Console.WriteLine($"Total: {summary.TotalTests} | Passed: {summary.PassedTests} | Failed: {summary.FailedTests}");
        Console.WriteLine($"Duration: {summary.Duration.TotalSeconds:F2}s");
        Console.WriteLine($"Test completed at: {summary.EndTime:yyyy-MM-dd HH:mm:ss.fff} UTC");
        Console.WriteLine();

        if (summary.FailedTests > 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("FAILED TESTS:");
            Console.ResetColor();
            
            foreach (var suite in summary.SuiteResults)
            {
                foreach (var result in suite.Results.Where(r => !r.Passed))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  [✗ FAIL] {result.TestName}: {result.Message}");
                    Console.ResetColor();
                }
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("All tests passed!");
            Console.ResetColor();
        }
    }
}


