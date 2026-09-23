namespace WalletSystem.Shared.Seeders;

/// <summary>
/// Shared test data for demo and seeding purposes.
/// This class exists outside the Clean Architecture layers to provide
/// common test data without violating the Dependency Rule.
/// </summary>
public static class SampleData
{
    
    public const string NULL_EMAIL = "null@local.loc";
    
    /// <summary>
    /// Gets all seeded users with their wallets.
    /// This data must match what DatabaseSeeder.cs creates.
    /// </summary>
    public static IReadOnlyList<SeededUser> GetSeededUsers()
    {
        return new List<SeededUser>
        {
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Email = "alice@example.com",
                Password = "Alice123!",
                FirstName = "Alice",
                LastName = "Johnson",
                Wallets = new[]
                {
                    new SeededWallet
                    {
                        Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                        Name = "Alice Main USD",
                        Currency = "USD",
                        InitialBalance = 5000.00m
                    },
                    new SeededWallet
                    {
                        Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                        Name = "Alice EUR Savings",
                        Currency = "EUR",
                        InitialBalance = 2500.50m
                    },
                    new SeededWallet
                    {
                        Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                        Name = "Alice GBP Travel",
                        Currency = "GBP",
                        InitialBalance = 1000.75m
                    }
                }
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Email = "bob@example.com",
                Password = "Bob123!",
                FirstName = "Bob",
                LastName = "Smith",
                Wallets = new[]
                {
                    new SeededWallet
                    {
                        Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
                        Name = "Bob Main USD",
                        Currency = "USD",
                        InitialBalance = 3500.00m
                    },
                    new SeededWallet
                    {
                        Id = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                        Name = "Bob Business USD",
                        Currency = "USD",
                        InitialBalance = 10000.00m
                    }
                }
            },
            new()
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Email = "charlie@example.com",
                Password = "Charlie123!",
                FirstName = "Charlie",
                LastName = "Brown",
                Wallets = new[]
                {
                    new SeededWallet
                    {
                        Id = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                        Name = "Charlie Main USD",
                        Currency = "USD",
                        InitialBalance = 750.25m
                    },
                    new SeededWallet
                    {
                        Id = Guid.Parse("11111111-1111-1111-1111-111111111112"),
                        Name = "Charlie CAD",
                        Currency = "CAD",
                        InitialBalance = 500.00m
                    }
                }
            },
            new()
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Email = "diana@example.com",
                Password = "Diana123!",
                FirstName = "Diana",
                LastName = "Williams",
                Wallets = new[]
                {
                    new SeededWallet
                    {
                        Id = Guid.Parse("22222222-2222-2222-2222-222222222223"),
                        Name = "Diana Main USD",
                        Currency = "USD",
                        InitialBalance = 8500.00m
                    },
                    new SeededWallet
                    {
                        Id = Guid.Parse("33333333-3333-3333-3333-333333333334"),
                        Name = "Diana EUR",
                        Currency = "EUR",
                        InitialBalance = 3200.00m
                    },
                    new SeededWallet
                    {
                        Id = Guid.Parse("44444444-4444-4444-4444-444444444445"),
                        Name = "Diana JPY",
                        Currency = "JPY",
                        InitialBalance = 150000.00m
                    }
                }
            }
        }.AsReadOnly();
    }

    /// <summary>
    /// Gets a user by email address
    /// </summary>
    public static SeededUser? GetUserByEmail(string email)
    {
        var users = GetSeededUsers();
        return users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets a wallet by ID
    /// </summary>
    public static SeededWallet? GetWalletById(Guid walletId)
    {
        var users = GetSeededUsers();
        return users
            .SelectMany(u => u.Wallets)
            .FirstOrDefault(w => w.Id == walletId);
    }

    /// <summary>
    /// Gets all wallets for a specific currency
    /// </summary>
    public static IReadOnlyList<SeededWallet> GetWalletsByCurrency(string currency)
    {
        var users = GetSeededUsers();
        return users
            .SelectMany(u => u.Wallets)
            .Where(w => w.Currency.Equals(currency, StringComparison.OrdinalIgnoreCase))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Gets two wallets with the same currency for transfer testing
    /// Returns null if no compatible pair exists
    /// </summary>
    public static (SeededWallet From, SeededWallet To)? GetCompatibleWalletPair(string currency)
    {
        var wallets = GetWalletsByCurrency(currency);
        if (wallets.Count < 2)
            return null;

        return (wallets[0], wallets[1]);
    }
}
