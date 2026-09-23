using System.Collections.Concurrent;
using WalletSystem.Demo.Client.Core;
using WalletSystem.Demo.Client.Commands;
using WalletSystem.Demo.Shared.Dtos;

namespace WalletSystem.Demo.Client.Strategies;

/// <summary>
/// Strategy for concurrency testing
/// Applied Strategy Pattern
/// </summary>
public sealed class ConcurrencyTestStrategy : ITestStrategy
{
    public string Name => "Concurrency";

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

            // Test concurrent deposits
            var depositAmount = 10.00m;
            var expectedTotal = initialBalance + (depositAmount * context.ConcurrencyLevel);
            var tasks = new List<Task<TransactionResponse>>();

            for (int i = 0; i < context.ConcurrencyLevel; i++)
            {
                var cmd = new DepositCommand(httpClient, context.WalletId, depositAmount, $"Concurrent deposit {i}");
                tasks.Add(cmd.ExecuteAsync(cancellationToken));
            }

            var depositResults = await Task.WhenAll(tasks);
            var successfulDeposits = depositResults.Count(r => r.IsCompleted);

            // Verify final balance
            var finalWallet = await getWalletCmd.ExecuteAsync(cancellationToken);

            if (successfulDeposits == context.ConcurrencyLevel && finalWallet.Balance == expectedTotal)
            {
                results.Add(new TestResult(
                    "Concurrency_ConcurrentDeposits",
                    true,
                    $"All {context.ConcurrencyLevel} concurrent deposits succeeded"
                ));
            }
            else
            {
                results.Add(new TestResult(
                    "Concurrency_ConcurrentDeposits",
                    false,
                    $"Expected {expectedTotal}, got {finalWallet.Balance}. Possible lost update!"
                ));
            }

            // Test double spending prevention
            var doubleSpendAmount = 10.00m;
            var reference = $"DOUBLE-{Guid.NewGuid():N}";
            
            var cmd1 = new WithdrawCommand(httpClient, context.WalletId, doubleSpendAmount, "First attempt", reference);
            var result1 = await cmd1.ExecuteAsync(cancellationToken);

            bool isIdempotent = false;
            try
            {
                var cmd2 = new WithdrawCommand(httpClient, context.WalletId, doubleSpendAmount, "Duplicate attempt", reference);
                var result2 = await cmd2.ExecuteAsync(cancellationToken);
                isIdempotent = result1.TransactionId == result2.TransactionId;
            }
            catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                isIdempotent = true; // Rejected is also correct behavior
            }

            results.Add(new TestResult(
                "Concurrency_DoubleSpending",
                isIdempotent,
                isIdempotent ? "Double spending prevented" : "WARNING: Double spend detected!"
            ));
        }
        catch (Exception ex)
        {
            results.Add(new TestResult("Concurrency_Setup", false, $"Setup error: {ex.Message}"));
        }

        var passed = results.All(r => r.Passed);
        return new TestSuiteResult(
            Name,
            passed,
            results,
            passed ? "All concurrency tests passed" : "Some concurrency tests failed"
        );
    }
}
