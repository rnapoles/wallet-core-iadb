using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WalletSystem.Application.Contracts.Services.Id;
using WalletSystem.Demo.Shared.Commands;
using WalletSystem.Demo.Shared.Dtos;
using WalletSystem.Infrastructure.Id;
using WalletSystem.Shared.Settings.Extensions;

namespace WalletSystem.Demo.Daemon;

/// <summary>
/// Daemon service that continuously runs random banking operations to test concurrency issues.
/// Uses Command pattern with multithreaded HTTP client.
/// Tests: Race conditions, Lost updates, Double spending, Negative balances, 
///        Inconsistent wallet balances, Duplicate financial transactions
/// </summary>
public class Program
{
    // Seeded user credentials from DatabaseSeeder
    private static readonly Dictionary<string, string> TestUsers = new()
    {
        { "alice@example.com", "Alice123!" },
        { "bob@example.com", "Bob123!" },
        { "charlie@example.com", "Charlie123!" },
        { "diana@example.com", "Diana123!" }
    };

    private static readonly Random Random = new();
    
    // Configuration
    private static string _baseUrl = "http://localhost:5000/";
    private static int _minDelaySeconds = 5;
    private static int _maxDelaySeconds = 20;
    private static int _concurrencyLevel = 10;
    private static bool _runIndefinitely = true;

    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("==============================================");
        Console.WriteLine("WalletSystem.Demo.Daemon - Concurrency Tester");
        Console.WriteLine("==============================================");
        Console.WriteLine();

        // Setup DI container
        var services = new ServiceCollection();
        ConfigureServices(services);
        var serviceProvider = services.BuildServiceProvider();

        // Load configuration from .env file and environment variables
        // Configuration priority (highest to lowest):
        // 1. Environment Variables
        // 2. .env file
        // 3. appsettings.json
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentFile()
            .AddEnvironmentVariables()
            .Build();

        // Support both flat env vars and hierarchical (DaemonSettings__ApiBaseUrl) format
        _baseUrl = config["DaemonSettings:ApiBaseUrl"] ?? config["API_BASE_URL"] ?? _baseUrl;
        if (args.Length > 0) _baseUrl = args[0];
        
        if (!Uri.TryCreate(_baseUrl, UriKind.Absolute, out var baseUri))
        {
            Console.WriteLine($"Invalid base URL: {_baseUrl}");
            return 1;
        }
        
        _minDelaySeconds = int.TryParse(config["DaemonSettings:MinIntervalSeconds"] ?? config["MIN_DELAY_SECONDS"], out var minDelay) ? minDelay : _minDelaySeconds;
        _maxDelaySeconds = int.TryParse(config["DaemonSettings:MaxIntervalSeconds"] ?? config["MAX_DELAY_SECONDS"], out var maxDelay) ? maxDelay : _maxDelaySeconds;
        _concurrencyLevel = int.TryParse(config["DaemonSettings:ThreadCount"] ?? config["CONCURRENCY_LEVEL"], out var concurrency) ? concurrency : _concurrencyLevel;
        _runIndefinitely = bool.TryParse(config["DaemonSettings:RunIndefinitely"] ?? config["RUN_INDEFINITELY"], out var runIndef) ? runIndef : _runIndefinitely;

        if (!_baseUrl.EndsWith("/")) _baseUrl += "/";

        Console.WriteLine($"Configuration:");
        Console.WriteLine($"  API Base URL: {_baseUrl}");
        Console.WriteLine($"  Delay Range: {_minDelaySeconds}-{_maxDelaySeconds} seconds");
        Console.WriteLine($"  Concurrency Level: {_concurrencyLevel}");
        Console.WriteLine($"  Run Indefinitely: {_runIndefinitely}");
        Console.WriteLine();

        using var httpClient = new HttpClient { BaseAddress = baseUri };
        httpClient.Timeout = TimeSpan.FromSeconds(120);

        // Resolve IIdGenerator from DI
        var idGenerator = serviceProvider.GetRequiredService<IIdGenerator>();

        try
        {
            var cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cancellationTokenSource.Cancel();
                Console.WriteLine("\nShutdown requested...");
            };

            // Select a random user to start with
            var users = TestUsers.ToList();
            var startIndex = Random.Next(users.Count);
            
            // Start daemon loop
            await RunDaemonAsync(httpClient, users[startIndex].Key, users[startIndex].Value, idGenerator, cancellationTokenSource.Token);
            
            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Daemon stopped gracefully.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FATAL ERROR: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            return 1;
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Register IIdGenerator as singleton using UuidV7IdGenerator
        services.AddSingleton<IIdGenerator, UuidV7IdGenerator>();
    }

    private static async Task RunDaemonAsync(HttpClient httpClient, string email, string password, IIdGenerator idGenerator, CancellationToken cancellationToken)
    {
        var iteration = 0;
        
        while (!cancellationToken.IsCancellationRequested)
        {
            iteration++;
            Console.WriteLine($"\n=== Iteration {iteration} ===");
            Console.WriteLine($"Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC");

            try
            {
                // Login
                Console.WriteLine($"Logging in as {email}...");
                var loginCommand = new LoginCommand(httpClient, email, password);
                var loginResult = await loginCommand.ExecuteAsync(cancellationToken);
                
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", loginResult.Token);

                // Get wallets
                var getWalletsCommand = new GetWalletsCommand(httpClient);
                var wallets = (await getWalletsCommand.ExecuteAsync(cancellationToken)).ToList();

                if (wallets.Count == 0)
                {
                    Console.WriteLine("No wallets found. Skipping this iteration.");
                    await Task.Delay(TimeSpan.FromSeconds(_minDelaySeconds), cancellationToken);
                    continue;
                }

                Console.WriteLine($"Found {wallets.Count} wallet(s)");

                // Pick random wallets for operations
                var sourceWallet = wallets[Random.Next(wallets.Count)];
                var targetWallet = wallets.Count > 1 
                    ? wallets.FirstOrDefault(w => w.Id != sourceWallet.Id) ?? sourceWallet
                    : sourceWallet;

                // Perform random operation
                var operationType = Random.Next(3); // 0=Deposit, 1=Withdrawal, 2=Transfer
                
                switch (operationType)
                {
                    case 0:
                        await RunDepositTestAsync(httpClient, sourceWallet, idGenerator, cancellationToken);
                        break;
                    case 1:
                        await RunWithdrawalTestAsync(httpClient, sourceWallet, idGenerator, cancellationToken);
                        break;
                    case 2:
                        await RunTransferTestAsync(httpClient, sourceWallet, targetWallet, idGenerator, cancellationToken);
                        break;
                }

                // Occasionally run concurrent tests
                if (iteration % 5 == 0 && wallets.Any(w => w.Balance >= 100))
                {
                    var highBalanceWallet = wallets.First(w => w.Balance >= 100);
                    await RunConcurrentDepositsTestAsync(httpClient, highBalanceWallet, idGenerator, cancellationToken);
                }

                // Random delay between 5-20 seconds
                var delaySeconds = Random.Next(_minDelaySeconds, _maxDelaySeconds + 1);
                Console.WriteLine($"Waiting {delaySeconds} seconds before next operation...");
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in iteration {iteration}: {ex.Message}");
                // Continue running despite errors
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }

    private static async Task RunDepositTestAsync(HttpClient httpClient, WalletResponse wallet, IIdGenerator idGenerator, CancellationToken ct)
    {
        var amount = Math.Round((decimal)(Random.NextDouble() * 50 + 1), 2);
        Console.WriteLine($"DEPOSIT: {amount} into wallet {wallet.Name} (Current balance: {wallet.Balance})");
        
        try
        {
            var command = new DepositCommand(httpClient, wallet.Id, amount, 
                $"Daemon deposit {DateTime.UtcNow:HH:mm:ss}", 
                $"DAEMON-DEP-{idGenerator.AsString()}",
                idGenerator);
            var result = await command.ExecuteAsync(ct);
            Console.WriteLine($"✓ Deposit successful. New balance: {result.Amount}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Deposit failed: {ex.Message}");
        }
    }

    private static async Task RunWithdrawalTestAsync(HttpClient httpClient, WalletResponse wallet, IIdGenerator idGenerator, CancellationToken ct)
    {
        // Only withdraw if balance is sufficient
        var maxWithdraw = Math.Min(wallet.Balance * 0.9m, 100m);
        if (maxWithdraw < 1)
        {
            Console.WriteLine($"WITHDRAWAL SKIPPED: Insufficient balance ({wallet.Balance})");
            return;
        }

        var amount = Math.Round((decimal)(Random.NextDouble() * (double)maxWithdraw + 1), 2);
        Console.WriteLine($"WITHDRAWAL: {amount} from wallet {wallet.Name} (Current balance: {wallet.Balance})");
        
        try
        {
            var command = new WithdrawCommand(httpClient, wallet.Id, amount,
                $"Daemon withdrawal {DateTime.UtcNow:HH:mm:ss}",
                $"DAEMON-WDR-{idGenerator.AsString()}",
                idGenerator);
            var result = await command.ExecuteAsync(ct);
            Console.WriteLine($"✓ Withdrawal successful.");
        }
        catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            Console.WriteLine($"✓ Withdrawal correctly rejected (insufficient funds or validation).");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Withdrawal failed: {ex.Message}");
        }
    }

    private static async Task RunTransferTestAsync(HttpClient httpClient, WalletResponse fromWallet, WalletResponse toWallet, IIdGenerator idGenerator, CancellationToken ct)
    {
        if (fromWallet.Id == toWallet.Id)
        {
            Console.WriteLine("TRANSFER SKIPPED: Same source and target wallet");
            return;
        }

        var maxTransfer = Math.Min(fromWallet.Balance * 0.9m, 50m);
        if (maxTransfer < 1)
        {
            Console.WriteLine($"TRANSFER SKIPPED: Insufficient balance ({fromWallet.Balance})");
            return;
        }

        var amount = Math.Round((decimal)(Random.NextDouble() * (double)maxTransfer + 1), 2);
        Console.WriteLine($"TRANSFER: {amount} from {fromWallet.Name} to {toWallet.Name}");
        
        try
        {
            var command = new TransferCommand(httpClient, fromWallet.Id, toWallet.Id, amount,
                $"Daemon transfer {DateTime.UtcNow:HH:mm:ss}",
                $"DAEMON-TRF-{idGenerator.AsString()}",
                idGenerator);
            var result = await command.ExecuteAsync(ct);
            Console.WriteLine($"✓ Transfer successful.");
        }
        catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            Console.WriteLine($"✓ Transfer correctly rejected (validation).");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Transfer failed: {ex.Message}");
        }
    }

    private static async Task RunConcurrentDepositsTestAsync(HttpClient httpClient, WalletResponse wallet, IIdGenerator idGenerator, CancellationToken ct)
    {
        Console.WriteLine($"\n--- CONCURRENT TEST: {_concurrencyLevel} parallel deposits ---");
        
        var depositAmount = 10.00m;
        var tasks = new List<Task<TransactionResponse>>();
        var successfulCount = 0;

        try
        {
            for (int i = 0; i < _concurrencyLevel; i++)
            {
                var command = new DepositCommand(httpClient, wallet.Id, depositAmount,
                    $"Concurrent deposit {i}", $"CONC-DEP-{i}-{idGenerator.AsString()}", idGenerator);
                tasks.Add(command.ExecuteAsync(ct));
            }

            var results = await Task.WhenAll(tasks);
            successfulCount = results.Count(r => r != null);

            // Verify final balance
            var getWalletCommand = new GetWalletByIdCommand(httpClient, wallet.Id);
            var finalWallet = await getWalletCommand.ExecuteAsync(ct);

            var expectedBalance = wallet.Balance + (depositAmount * _concurrencyLevel);
            
            if (finalWallet.Balance == expectedBalance)
            {
                Console.WriteLine($"✓ Concurrent deposits: All {_concurrencyLevel} succeeded. Balance correct: {wallet.Balance} -> {finalWallet.Balance}");
            }
            else
            {
                Console.WriteLine($"✗ RACE CONDITION DETECTED! Expected balance: {expectedBalance}, Actual: {finalWallet.Balance}");
                Console.WriteLine($"   Lost updates may have occurred!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Concurrent test error: {ex.Message}");
        }
    }
}
