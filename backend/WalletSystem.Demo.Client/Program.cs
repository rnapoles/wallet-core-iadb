using Microsoft.Extensions.DependencyInjection;
using WalletSystem.Application.Contracts.Services.Id;
using WalletSystem.Demo.Client.Core;
using WalletSystem.Demo.Client.Facades;
using WalletSystem.Demo.Client.Observers;
using WalletSystem.Demo.Shared.Dtos;
using WalletSystem.Infrastructure.Id;
using WalletSystem.Shared;
using WalletSystem.Shared.Seeders;

namespace WalletSystem.Demo.Client;

/// <summary>
/// Represents the result of a cross-user transfer attempt
/// </summary>
public sealed record TransferAttemptResult(
    string FromEmail,
    string ToEmail,
    decimal Amount,
    string Description,
    bool Success,
    TransactionResponse? Response = null,
    string? ErrorMessage = null,
    int? HttpStatusCode = null,
    string? ResponseContent = null,
    string? Endpoint = null,
    ExceptionInfo? ExceptionDetails = null
);

/// <summary>
/// Main program for HTTP-based wallet system testing
/// Uses an architecture with GoF design patterns:
/// - Facade Pattern: TestRunnerFacade simplifies the testing subsystem
/// - Command Pattern: Encapsulated API requests as command objects
/// - Strategy Pattern: Interchangeable test algorithms
/// - Observer Pattern: Decoupled event notifications
/// - Factory Method Pattern: Centralized strategy creation
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine("WalletSystem HTTP Demo & Concurrency Tests");
        Console.WriteLine("===========================================");
        Console.WriteLine();

        // Initialize configuration from args or environment
        ClientConfiguration.Initialize(args);

        // Setup DI container
        var services = new ServiceCollection();
        services.AddSingleton<IIdGenerator, UuidV7IdGenerator>();
        var serviceProvider = services.BuildServiceProvider();

        // Resolve IIdGenerator from DI
        var idGenerator = serviceProvider.GetRequiredService<IIdGenerator>();

        // Get API base URL from shared configuration
        var baseUrl = ClientConfiguration.ApiBaseUrl;

        Console.WriteLine($"API Base URL: {baseUrl}");
        Console.WriteLine($"Test started at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC");
        Console.WriteLine();

        using var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(baseUrl);
        httpClient.Timeout = TimeSpan.FromSeconds(120);

        var allTransferResults = new List<TransferAttemptResult>();
        var allTestResults = new List<TestRunSummary>();

        try
        {
            // Get users from shared SampleData
            var alice = SampleData.GetUserByEmail("alice@example.com")!;
            var bob = SampleData.GetUserByEmail("bob@example.com")!;
            var charlie = SampleData.GetUserByEmail("charlie@example.com")!;
            var diana = SampleData.GetUserByEmail("diana@example.com")!;

            // Create observer (can be swapped for different logging strategies)
            ITestObserver observer = new ConsoleTestObserver();

            // Create facade with observer
            var facade = new TestRunnerFacade(httpClient, observer);

            // ============================================
            // PHASE 1: Cross-User Transfer Demonstrations
            // ============================================
            Console.WriteLine("=== PHASE 1: Cross-User Money Transfers ===");
            Console.WriteLine();

            // Transfer 1: Bob sends $500 to Alice (Bob's Main USD -> Alice's Main USD)
            var transfer1Result = await PerformCrossUserTransferAsync(
                httpClient,
                fromEmail: bob.Email,
                fromPassword: bob.Password,
                fromWalletId: bob.Wallets[0].Id,
                toEmail: alice.Email,
                toPassword: alice.Password,
                toWalletId: alice.Wallets[0].Id,
                amount: 500.00m,
                description: "Bob pays Alice for dinner"
            );
            allTransferResults.Add(transfer1Result);
            PrintTransferResults(transfer1Result);
            Console.WriteLine();

            httpClient.DefaultRequestHeaders.Authorization = null;

            // Transfer 2: Diana sends €100 to Alice (Diana's EUR -> Alice's EUR Savings)
            var transfer2Result = await PerformCrossUserTransferAsync(
                httpClient,
                fromEmail: diana.Email,
                fromPassword: diana.Password,
                fromWalletId: diana.Wallets.First(w => w.Currency == "EUR").Id,
                toEmail: alice.Email,
                toPassword: alice.Password,
                toWalletId: alice.Wallets.First(w => w.Currency == "EUR").Id,
                amount: 100.00m,
                description: "Diana repays Alice for concert tickets"
            );
            allTransferResults.Add(transfer2Result);
            PrintTransferResults(transfer2Result);
            Console.WriteLine();

            httpClient.DefaultRequestHeaders.Authorization = null;

            // Transfer 3: Charlie sends $200 to Bob (Charlie's Main USD -> Bob's Main USD)
            var transfer3Result = await PerformCrossUserTransferAsync(
                httpClient,
                fromEmail: charlie.Email,
                fromPassword: charlie.Password,
                fromWalletId: charlie.Wallets[0].Id,
                toEmail: bob.Email,
                toPassword: bob.Password,
                toWalletId: bob.Wallets[0].Id,
                amount: 200.00m,
                description: "Charlie reimburses Bob for office supplies"
            );
            allTransferResults.Add(transfer3Result);
            PrintTransferResults(transfer3Result);
            Console.WriteLine();

            httpClient.DefaultRequestHeaders.Authorization = null;

            // ============================================
            // PHASE 2: Individual User Testing with Facade
            // ============================================
            Console.WriteLine("=== PHASE 2: Individual User Testing ===");
            Console.WriteLine();

            var allResults = new List<TestRunSummary>();

            // Test with Alice
            Console.WriteLine("--- Testing with Alice ---");
            var aliceContext = new TestContext(
                Email: alice.Email,
                Password: alice.Password,
                WalletId: alice.Wallets[0].Id,
                InitialBalance: 0m,
                Currency: "USD"
            );
            var aliceSummary = await facade.RunAllAsync(aliceContext);
            allResults.Add(aliceSummary);
            allTestResults.Add(aliceSummary);
            Console.WriteLine();

            // Test with Bob
            Console.WriteLine("--- Testing with Bob ---");
            var bobContext = new TestContext(
                Email: bob.Email,
                Password: bob.Password,
                WalletId: bob.Wallets[0].Id,
                InitialBalance: 0m,
                Currency: "USD"
            );
            var bobSummary = await facade.RunAllAsync(bobContext);
            allResults.Add(bobSummary);
            allTestResults.Add(bobSummary);
            Console.WriteLine();

            // Test with Charlie
            Console.WriteLine("--- Testing with Charlie ---");
            var charlieContext = new TestContext(
                Email: charlie.Email,
                Password: charlie.Password,
                WalletId: charlie.Wallets[0].Id,
                InitialBalance: 0m,
                Currency: "USD"
            );
            var charlieSummary = await facade.RunAllAsync(charlieContext);
            allResults.Add(charlieSummary);
            allTestResults.Add(charlieSummary);
            Console.WriteLine();

            // Test with Diana
            Console.WriteLine("--- Testing with Diana ---");
            var dianaContext = new TestContext(
                Email: diana.Email,
                Password: diana.Password,
                WalletId: diana.Wallets.First(w => w.Currency == "USD").Id,
                InitialBalance: 0m,
                Currency: "USD"
            );
            var dianaSummary = await facade.RunAllAsync(dianaContext);
            allResults.Add(dianaSummary);
            allTestResults.Add(dianaSummary);
            Console.WriteLine();

            // ============================================
            // FINAL SUMMARY
            // ============================================
            Console.WriteLine("===========================================");
            Console.WriteLine("FINAL SUMMARY - ALL USERS");
            Console.WriteLine("===========================================");
            
            var totalTests = allResults.Sum(r => r.TotalTests);
            var totalPassed = allResults.Sum(r => r.PassedTests);
            var totalFailed = allResults.Sum(r => r.FailedTests);
            var overallSuccess = totalFailed == 0;

            Console.WriteLine($"Total Tests: {totalTests}");
            Console.WriteLine($"Passed: {totalPassed}");
            Console.WriteLine($"Failed: {totalFailed}");
            
            if (overallSuccess)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("✓ All tests passed!");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("✗ Some tests failed!");
                Console.ResetColor();
            }

            // Print Phase 1 Transfer Summary
            PrintTransferSummary(allTransferResults);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Fatal error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Console.ResetColor();
            
            // Still print any results we have before exiting
            if (allTransferResults.Count > 0 || allTestResults.Count > 0)
            {
                PrintTransferSummary(allTransferResults);
                PrintTestResultsSummary(allTestResults);
            }
            
            return 1;
        }

        return 0;
    }

    /// <summary>
    /// Perform a cross-user transfer using the Command pattern
    /// Returns result object even on failure to allow test continuation
    /// </summary>
    private static async Task<TransferAttemptResult> PerformCrossUserTransferAsync(
        HttpClient httpClient,
        string fromEmail,
        string fromPassword,
        Guid fromWalletId,
        string toEmail,
        string toPassword,
        Guid toWalletId,
        decimal amount,
        string description)
    {
        Console.WriteLine($"Transferring {amount:C} from {fromEmail} to {toEmail}");
        Console.WriteLine($"Description: {description}");

        try
        {
            // Login as sender
            var loginCommand = new Commands.LoginCommand(httpClient, fromEmail, fromPassword);
            var loginResult = await loginCommand.ExecuteAsync();
            
            if (loginResult.Token == null)
            {
                var failResult = new TransferAttemptResult(
                    FromEmail: fromEmail,
                    ToEmail: toEmail,
                    Amount: amount,
                    Description: description,
                    Success: false,
                    ErrorMessage: $"Failed to login as {fromEmail}"
                );
                return failResult;
            }

            // Set the authorization header for subsequent requests
            httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult.Token);

            // Perform transfer
            var transferCommand = new Commands.TransferCommand(
                httpClient,
                fromWalletId,
                toWalletId,
                amount,
                description);
            
            var transferResponse = await transferCommand.ExecuteAsync();
            
            return new TransferAttemptResult(
                FromEmail: fromEmail,
                ToEmail: toEmail,
                Amount: amount,
                Description: description,
                Success: transferResponse.IsCompleted,
                Response: transferResponse,
                Endpoint: "POST api/transactions/transfer"
            );
        }
        catch (HttpRequestException httpEx)
        {
            var errorContent = await GetErrorContentAsync(httpClient);
            return new TransferAttemptResult(
                FromEmail: fromEmail,
                ToEmail: toEmail,
                Amount: amount,
                Description: description,
                Success: false,
                ErrorMessage: httpEx.Message,
                HttpStatusCode: (int?)httpEx.StatusCode,
                ResponseContent: errorContent,
                Endpoint: "POST api/transactions/transfer",
                ExceptionDetails: ExceptionInfo.FromException(httpEx)
            );
        }
        catch (Exception ex)
        {
            return new TransferAttemptResult(
                FromEmail: fromEmail,
                ToEmail: toEmail,
                Amount: amount,
                Description: description,
                Success: false,
                ErrorMessage: ex.Message,
                Endpoint: "POST api/transactions/transfer",
                ExceptionDetails: ExceptionInfo.FromException(ex)
            );
        }
    }

    /// <summary>
    /// Helper to read error content from HTTP client after an exception
    /// </summary>
    private static async Task<string> GetErrorContentAsync(HttpClient httpClient)
    {
        try
        {
            // Try to get any pending response content
            return await httpClient.GetStringAsync("api/health/check");
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Print transfer results
    /// </summary>
    private static void PrintTransferResults(TransferAttemptResult result)
    {
        if (result.Success && result.Response != null)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✓ Transfer successful: {result.Response.TransactionId:D}");
            Console.ResetColor();
            Console.WriteLine($"  Type: {result.Response.Type}");
            Console.WriteLine($"  Amount: {result.Response.Amount:C}");
            Console.WriteLine($"  New Balance: {result.Response.NewBalance:C}");
            Console.WriteLine($"  Message: {result.Response.Message}");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"✗ Transfer failed: {result.ErrorMessage ?? "Unknown error"}");
            Console.ResetColor();
            if (!string.IsNullOrEmpty(result.ResponseContent))
            {
                Console.WriteLine($"  Server Response: {result.ResponseContent}");
            }
            if (result.HttpStatusCode.HasValue)
            {
                Console.WriteLine($"  HTTP Status Code: {result.HttpStatusCode}");
            }
            if (result.ExceptionDetails != null)
            {
                Console.WriteLine($"  Error Type: {result.ExceptionDetails.Type}");
            }
        }
    }

    /// <summary>
    /// Print detailed summary of all transfer attempts
    /// </summary>
    private static void PrintTransferSummary(IReadOnlyList<TransferAttemptResult> results)
    {
        Console.WriteLine();
        Console.WriteLine("===========================================");
        Console.WriteLine("PHASE 1: TRANSFER ATTEMPTS DETAILED REPORT");
        Console.WriteLine("===========================================");
        
        int successCount = results.Count(r => r.Success);
        int failCount = results.Count(r => !r.Success);
        
        Console.WriteLine($"Total Attempts: {results.Count}");
        Console.WriteLine($"Successful: {successCount}");
        Console.WriteLine($"Failed: {failCount}");
        Console.WriteLine();
        
        foreach (var result in results)
        {
            Console.WriteLine("-------------------------------------------");
            Console.WriteLine($"From: {result.FromEmail}");
            Console.WriteLine($"To: {result.ToEmail}");
            Console.WriteLine($"Amount: {result.Amount:C}");
            Console.WriteLine($"Description: {result.Description}");
            Console.WriteLine($"Status: {(result.Success ? "✓ SUCCESS" : "✗ FAILED")}");
            
            if (result.Success && result.Response != null)
            {
                Console.WriteLine($"Transaction ID: {result.Response.TransactionId:D}");
                Console.WriteLine($"Type: {result.Response.Type}");
                Console.WriteLine($"New Balance: {result.Response.NewBalance:C}");
            }
            else
            {
                Console.WriteLine($"Error: {result.ErrorMessage ?? "Unknown"}");
                if (result.HttpStatusCode.HasValue)
                {
                    Console.WriteLine($"HTTP Status Code: {result.HttpStatusCode}");
                }
                if (!string.IsNullOrEmpty(result.ResponseContent))
                {
                    Console.WriteLine($"Server Response: {result.ResponseContent}");
                }
                if (result.ExceptionDetails != null)
                {
                    Console.WriteLine($"Exception Type: {result.ExceptionDetails.Type}");
                    Console.WriteLine($"Exception Message: {result.ExceptionDetails.Message}");
                }
            }
            Console.WriteLine($"Endpoint: {result.Endpoint ?? "N/A"}");
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Print summary of test results from Phase 2
    /// </summary>
    private static void PrintTestResultsSummary(IReadOnlyList<TestRunSummary> summaries)
    {
        Console.WriteLine();
        Console.WriteLine("===========================================");
        Console.WriteLine("PHASE 2: TEST SUITE DETAILED REPORT");
        Console.WriteLine("===========================================");
        
        foreach (var summary in summaries)
        {
            Console.WriteLine($"Test Run: {summary.StartTime:yyyy-MM-dd HH:mm:ss} UTC");
            Console.WriteLine($"Duration: {summary.Duration.TotalSeconds:F2}s");
            Console.WriteLine($"Total: {summary.TotalTests} | Passed: {summary.PassedTests} | Failed: {summary.FailedTests}");
            Console.WriteLine();
            
            foreach (var suite in summary.SuiteResults)
            {
                Console.WriteLine($"  Suite: {suite.SuiteName} - {(suite.Passed ? "PASSED" : "FAILED")}");
                foreach (var result in suite.Results)
                {
                    var statusIcon = result.Passed ? "✓" : "✗";
                    Console.WriteLine($"    [{statusIcon}] {result.TestName}: {result.Message}");
                    if (!result.Passed)
                    {
                        if (!string.IsNullOrEmpty(result.ErrorCode))
                        {
                            Console.WriteLine($"        Error Code: {result.ErrorCode}");
                        }
                        if (result.HttpStatusCode.HasValue)
                        {
                            Console.WriteLine($"        HTTP Status: {result.HttpStatusCode}");
                        }
                        if (result.ExceptionDetails != null)
                        {
                            Console.WriteLine($"        Exception: {result.ExceptionDetails.Type} - {result.ExceptionDetails.Message}");
                        }
                    }
                }
            }
            Console.WriteLine();
        }
    }
}
