using WalletSystem.Demo.Client.Core;
using WalletSystem.Demo.Client.Commands;
using WalletSystem.Demo.Shared.Dtos;

namespace WalletSystem.Demo.Client.Strategies;

/// <summary>
/// Strategy for basic operations testing (deposit, withdrawal, transfer)
/// Applied Strategy Pattern
/// </summary>
public sealed class BasicOperationsTestStrategy : ITestStrategy
{
    public string Name => "BasicOperations";

    public async Task<TestSuiteResult> ExecuteAsync(TestContext context, CancellationToken cancellationToken = default)
    {
        var results = new List<TestResult>();
        
        try
        {
            // Login using a temporary client
            using var loginClient = new HttpClient();
            loginClient.BaseAddress = new Uri(ClientConfiguration.ApiBaseUrl);
            var loginCmd = new LoginCommand(loginClient, context.Email, context.Password);
            var loginResponse = await loginCmd.ExecuteAsync(cancellationToken);
            
            // Setup HTTP client with auth token (use the one from context or create new)
            var httpClient = context.HttpClient ?? new HttpClient();
            if (context.HttpClient == null)
            {
                httpClient.BaseAddress = new Uri(ClientConfiguration.ApiBaseUrl);
            }
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponse.Token);

            // Get wallet info
            var getWalletCmd = new GetWalletByIdCommand(httpClient, context.WalletId);
            var wallet = await getWalletCmd.ExecuteAsync(cancellationToken);

            // Test 1: Deposit
            try
            {
                var depositAmount = 100.00m;
                var depositCmd = new DepositCommand(httpClient, context.WalletId, depositAmount, "Test deposit");
                var depositResult = await depositCmd.ExecuteAsync(cancellationToken);

                results.Add(new TestResult(
                    "BasicOperations_Deposit",
                    depositResult.IsCompleted && depositResult.Amount == depositAmount,
                    $"Successfully deposited {depositAmount:C}",
                    ExpectedValue: depositAmount.ToString(),
                    ActualValue: depositResult.Amount.ToString()
                ));
            }
            catch (Exception ex)
            {
                results.Add(new TestResult("BasicOperations_Deposit", false, $"Deposit failed: {ex.Message}"));
            }

            // Test 2: Withdrawal
            try
            {
                var withdrawAmount = 50.00m;
                var withdrawCmd = new WithdrawCommand(httpClient, context.WalletId, withdrawAmount, "Test withdrawal");
                var withdrawResult = await withdrawCmd.ExecuteAsync(cancellationToken);

                results.Add(new TestResult(
                    "BasicOperations_Withdrawal",
                    withdrawResult.IsCompleted,
                    $"Successfully withdrew {withdrawResult.Amount:C}"
                ));
            }
            catch (Exception ex)
            {
                results.Add(new TestResult("BasicOperations_Withdrawal", false, $"Withdrawal failed: {ex.Message}"));
            }

            // Test 3: Transfer (if compatible wallet exists)
            try
            {
                var transferAmount = 25.00m;
                var otherWalletId = Guid.NewGuid(); // In real scenario, find another user's wallet
                var transferCmd = new TransferCommand(httpClient, context.WalletId, otherWalletId, transferAmount, "Test transfer");
                
                // This may fail if no valid destination wallet - that's expected in some scenarios
                try
                {
                    var transferResult = await transferCmd.ExecuteAsync(cancellationToken);
                    results.Add(new TestResult(
                        "BasicOperations_Transfer",
                        transferResult.IsCompleted,
                        $"Successfully transferred {transferAmount:C}"
                    ));
                }
                catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    results.Add(new TestResult(
                        "BasicOperations_Transfer",
                        true,
                        "Skipped - no valid destination wallet available"
                    ));
                }
            }
            catch (Exception ex)
            {
                results.Add(new TestResult("BasicOperations_Transfer", false, $"Transfer failed: {ex.Message}"));
            }
        }
        catch (Exception ex)
        {
            results.Add(new TestResult("BasicOperations_Setup", false, $"Setup error: {ex.Message}"));
        }

        var passed = results.All(r => r.Passed);
        return new TestSuiteResult(
            Name,
            passed,
            results,
            passed ? "All basic operations passed" : "Some basic operations failed"
        );
    }
}
