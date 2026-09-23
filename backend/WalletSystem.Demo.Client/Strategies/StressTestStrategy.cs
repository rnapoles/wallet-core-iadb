using System.Collections.Concurrent;
using WalletSystem.Demo.Client.Core;
using WalletSystem.Demo.Client.Commands;
using WalletSystem.Demo.Shared.Dtos;

namespace WalletSystem.Demo.Client.Strategies;

/// <summary>
/// Strategy for stress testing with high concurrency
/// Applied Strategy Pattern
/// </summary>
public sealed class StressTestStrategy : ITestStrategy
{
    public string Name => "Stress";

    public async Task<TestSuiteResult> ExecuteAsync(TestContext context, CancellationToken cancellationToken = default)
    {
        var results = new List<TestResult>();
        
        try
        {
            using var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(ClientConfiguration.ApiBaseUrl);
            
            // Login
            var loginCmd = new LoginCommand(httpClient, context.Email, context.Password);
            var loginResponse = await loginCmd.ExecuteAsync(cancellationToken);
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponse.Token);

            // Get initial wallet state
            var getWalletCmd = new GetWalletByIdCommand(httpClient, context.WalletId);
            var wallet = await getWalletCmd.ExecuteAsync(cancellationToken);
            var initialBalance = wallet.Balance;

            Console.WriteLine($"Starting stress test with {context.RequestCount} requests, parallelism: {context.DegreeOfParallelism}");

            var semaphore = new SemaphoreSlim(context.DegreeOfParallelism);
            var operationResults = new ConcurrentBag<(string Operation, bool Success)>();

            var tasks = new List<Task>();
            var depositCount = context.RequestCount / 3;
            var withdrawCount = context.RequestCount / 3;
            var transferCount = context.RequestCount - depositCount - withdrawCount;

            // Create deposit tasks
            for (int i = 0; i < depositCount; i++)
            {
                tasks.Add(RunWithSemaphore(async () =>
                {
                    try
                    {
                        var cmd = new DepositCommand(httpClient, context.WalletId, 1.00m, $"Stress deposit {i}");
                        await cmd.ExecuteAsync(cancellationToken);
                        operationResults.Add(("Deposit", true));
                    }
                    catch
                    {
                        operationResults.Add(("Deposit", false));
                    }
                }, semaphore));
            }

            // Create withdrawal tasks
            for (int i = 0; i < withdrawCount; i++)
            {
                tasks.Add(RunWithSemaphore(async () =>
                {
                    try
                    {
                        var cmd = new WithdrawCommand(httpClient, context.WalletId, 0.50m, $"Stress withdrawal {i}");
                        await cmd.ExecuteAsync(cancellationToken);
                        operationResults.Add(("Withdrawal", true));
                    }
                    catch
                    {
                        operationResults.Add(("Withdrawal", false));
                    }
                }, semaphore));
            }

            await Task.WhenAll(tasks);

            // Final balance check
            var finalWallet = await getWalletCmd.ExecuteAsync(cancellationToken);
            
            var successCount = operationResults.Count(r => r.Success);
            var failCount = operationResults.Count(r => !r.Success);

            results.Add(new TestResult(
                "Stress_MixedOperations",
                successCount > 0,
                $"Completed: {successCount}/{context.RequestCount} successful, {failCount} failed"
            ));

            results.Add(new TestResult(
                "Stress_NegativeBalanceCheck",
                finalWallet.Balance >= 0,
                finalWallet.Balance >= 0 
                    ? $"Balance remained non-negative: {finalWallet.Balance}" 
                    : $"CRITICAL: Negative balance! {finalWallet.Balance}"
            ));
        }
        catch (Exception ex)
        {
            results.Add(new TestResult("Stress_Setup", false, $"Setup error: {ex.Message}"));
        }

        var passed = results.All(r => r.Passed);
        return new TestSuiteResult(
            Name,
            passed,
            results,
            passed ? "Stress test completed successfully" : "Stress test detected issues"
        );
    }

    private static async Task RunWithSemaphore(Func<Task> operation, SemaphoreSlim semaphore)
    {
        await semaphore.WaitAsync();
        try
        {
            await operation();
        }
        finally
        {
            semaphore.Release();
        }
    }
}
