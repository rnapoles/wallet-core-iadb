using Microsoft.EntityFrameworkCore;
using WalletSystem.Application.Contracts.Services.Id;
using WalletSystem.Application.Contracts.Services.Security;
using WalletSystem.Domain.Entities.Transactions;
using WalletSystem.Domain.Entities.Users;
using WalletSystem.Domain.Entities.Wallets;
using WalletSystem.Shared.Seeders;

namespace WalletSystem.Infrastructure.Persistence.Seeding;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, IPasswordHasher passwordHasher, IIdGenerator? idGenerator)
    {
        // Check if data already exists
        if (await context.Users.AnyAsync())
        {
            return; // Data already seeded
        }

        var seededUsers = SampleData.GetSeededUsers();

        // Create demo users from shared test data
        var users = new List<User>();
        foreach (var seededUser in seededUsers)
        {
            users.Add(new User
            {
                Id = seededUser.Id,
                Email = seededUser.Email,
                PasswordHash = passwordHasher.HashPassword(seededUser.Password),
                FirstName = seededUser.FirstName,
                LastName = seededUser.LastName,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                IsActive = true
            });
        }

        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();

        // Create wallets for each user from shared test data
        var wallets = new List<Wallet>();
        foreach (var seededUser in seededUsers)
        {
            var user = users.First(u => u.Id == seededUser.Id);
            foreach (var seededWallet in seededUser.Wallets)
            {
                wallets.Add(new Wallet
                {
                    Id = seededWallet.Id,
                    Name = seededWallet.Name,
                    Currency = seededWallet.Currency,
                    Balance = seededWallet.InitialBalance,
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    LastTransactionAt = DateTime.UtcNow.AddDays(-1)
                });
            }
        }

        await context.Wallets.AddRangeAsync(wallets);
        await context.SaveChangesAsync();

        var transactions = new List<Transaction>();
        var now = DateTime.UtcNow;

        // Helper to get wallet by ID
        Wallet GetWallet(Guid id) => wallets.First(w => w.Id == id);

        var aliceUsdWallet = GetWallet(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var aliceEurWallet = GetWallet(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
        var aliceGbpWallet = GetWallet(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));
        var bobUsdWallet = GetWallet(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"));
        var bobBusinessWallet = GetWallet(Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"));
        var charlieUsdWallet = GetWallet(Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"));
        var charlieCadWallet = GetWallet(Guid.Parse("11111111-1111-1111-1111-111111111112"));
        var dianaUsdWallet = GetWallet(Guid.Parse("22222222-2222-2222-2222-222222222223"));
        var dianaEurWallet = GetWallet(Guid.Parse("33333333-3333-3333-3333-333333333334"));
        var dianaJpyWallet = GetWallet(Guid.Parse("44444444-4444-4444-4444-444444444445"));

        // Helper to generate IDs using IIdGenerator
        Guid GenerateId()
        {
            if (idGenerator == null)
            {
                throw new InvalidOperationException("IIdGenerator is required for database seeding but was not provided.");
            }
            return idGenerator.CreateId();
        }

        // Alice's transactions
        transactions.Add(new()
        {
            Id = GenerateId(),
            Type = TransactionType.Deposit,
            Amount = 1000.00m,
            Description = "Initial deposit",
            WalletId = aliceUsdWallet.Id,
            Reference = "DEP-001-" + GenerateId().ToString("N"),
            CreatedAt = now.AddDays(-30),
            IsCompleted = true
        });
        transactions.Add(new()
        {
            Id = GenerateId(),
            Type = TransactionType.Deposit,
            Amount = 2500.00m,
            Description = "Salary deposit",
            WalletId = aliceUsdWallet.Id,
            Reference = "DEP-002-" + GenerateId().ToString("N"),
            CreatedAt = now.AddDays(-20),
            IsCompleted = true
        });
        transactions.Add(new()
        {
            Id = GenerateId(),
            Type = TransactionType.Deposit,
            Amount = 1500.00m,
            Description = "Freelance payment",
            WalletId = aliceUsdWallet.Id,
            Reference = "DEP-003-" + GenerateId().ToString("N"),
            CreatedAt = now.AddDays(-10),
            IsCompleted = true
        });
        transactions.Add(new()
        {
            Id = GenerateId(),
            Type = TransactionType.Withdrawal,
            Amount = 500.00m,
            Description = "ATM withdrawal",
            WalletId = aliceUsdWallet.Id,
            Reference = "WDR-001-" + GenerateId().ToString("N"),
            CreatedAt = now.AddDays(-15),
            IsCompleted = true
        });
        transactions.Add(new()
        {
            Id = GenerateId(),
            Type = TransactionType.Deposit,
            Amount = 2500.50m,
            Description = "EUR savings transfer",
            WalletId = aliceEurWallet.Id,
            Reference = "DEP-004-" + GenerateId().ToString("N"),
            CreatedAt = now.AddDays(-25),
            IsCompleted = true
        });
        transactions.Add(new()
        {
            Id = GenerateId(),
            Type = TransactionType.Deposit,
            Amount = 1000.75m,
            Description = "Travel fund",
            WalletId = aliceGbpWallet.Id,
            Reference = "DEP-005-" + GenerateId().ToString("N"),
            CreatedAt = now.AddDays(-20),
            IsCompleted = true
        });
        // Bob's transactions
        transactions.Add(new()
        {
            Id = GenerateId(),
            Type = TransactionType.Deposit,
            Amount = 3500.00m,
            Description = "Initial funding",
            WalletId = bobUsdWallet.Id,
            Reference = "DEP-006-" + GenerateId().ToString("N"),
            CreatedAt = now.AddDays(-25),
            IsCompleted = true
        });
        transactions.Add(new()
        {
            Id = GenerateId(),
            Type = TransactionType.Deposit,
            Amount = 10000.00m,
            Description = "Business investment",
            WalletId = bobBusinessWallet.Id,
            Reference = "DEP-007-" + GenerateId().ToString("N"),
            CreatedAt = now.AddDays(-20),
            IsCompleted = true
        });
        transactions.Add(new()
        {
            Id = GenerateId(),
            Type = TransactionType.Withdrawal,
            Amount = 1500.00m,
            Description = "Business expense",
            WalletId = bobBusinessWallet.Id,
            Reference = "WDR-002-" + GenerateId().ToString("N"),
            CreatedAt = now.AddDays(-10),
            IsCompleted = true
        });
        // Transfer from Bob to Alice
        transactions.Add(new()
        {
            Id = GenerateId(),
            Type = TransactionType.TransferOut,
            Amount = 500.00m,
            Description = "Transfer to Alice",
            WalletId = bobUsdWallet.Id,
            RelatedWalletId = aliceUsdWallet.Id,
            Reference = "TRF-001-" + GenerateId().ToString("N"),
            CreatedAt = now.AddDays(-5),
            IsCompleted = true
        });
        transactions.Add(new()
        {
            Id = GenerateId(),
            Type = TransactionType.TransferIn,
            Amount = 500.00m,
            Description = "Transfer from Bob",
            WalletId = aliceUsdWallet.Id,
            RelatedWalletId = bobUsdWallet.Id,
            Reference = "TRF-001-" + GenerateId().ToString("N"),
            CreatedAt = now.AddDays(-5),
            IsCompleted = true
        });
        // Charlie's transactions
        transactions.Add(new()
        {
            Id = GenerateId(),
            Type = TransactionType.Deposit,
            Amount = 750.25m,
            Description = "Part-time job payment",
            WalletId = charlieUsdWallet.Id,
            Reference = "DEP-008-" + GenerateId().ToString("N"),
            CreatedAt = now.AddDays(-20),
            IsCompleted = true
        });
        transactions.Add(new()
        {
            Id = GenerateId(),
            Type = TransactionType.Deposit,
            Amount = 500.00m,
            Description = "CAD exchange",
            WalletId = charlieCadWallet.Id,
            Reference = "DEP-009-" + GenerateId().ToString("N"),
            CreatedAt = now.AddDays(-15),
            IsCompleted = true
        });
        transactions.Add(new()
        {
            Id = GenerateId(),
            Type = TransactionType.Withdrawal,
            Amount = 200.00m,
            Description = "Online purchase",
            WalletId = charlieUsdWallet.Id,
            Reference = "WDR-003-" + GenerateId().ToString("N"),
            CreatedAt = now.AddDays(-8),
            IsCompleted = true
        });
        // Diana's transactions
        transactions.Add(new()
        {
            Id = GenerateId(),
            Type = TransactionType.Deposit,
            Amount = 8500.00m,
            Description = "Consulting fee",
            WalletId = dianaUsdWallet.Id,
            Reference = "DEP-010-" + GenerateId().ToString("N"),
            CreatedAt = now.AddDays(-15),
            IsCompleted = true
        });
        transactions.Add(new()
        {
            Id = GenerateId(),
            Type = TransactionType.Deposit,
            Amount = 3200.00m,
            Description = "EUR client payment",
            WalletId = dianaEurWallet.Id,
            Reference = "DEP-011-" + GenerateId().ToString("N"),
            CreatedAt = now.AddDays(-10),
            IsCompleted = true
        });
        transactions.Add(new()
        {
            Id = GenerateId(),
            Type = TransactionType.Deposit,
            Amount = 150000.00m,
            Description = "JPY project payment",
            WalletId = dianaJpyWallet.Id,
            Reference = "DEP-012-" + GenerateId().ToString("N"),
            CreatedAt = now.AddDays(-5),
            IsCompleted = true
        });
        transactions.Add(new()
        {
            Id = GenerateId(),
            Type = TransactionType.Withdrawal,
            Amount = 1000.00m,
            Description = "Equipment purchase",
            WalletId = dianaUsdWallet.Id,
            Reference = "WDR-004-" + GenerateId().ToString("N"),
            CreatedAt = now.AddDays(-7),
            IsCompleted = true
        });
        // Transfer from Diana to Charlie
        transactions.Add(new()
        {
            Id = GenerateId(),
            Type = TransactionType.TransferOut,
            Amount = 300.00m,
            Description = "Loan repayment",
            WalletId = dianaUsdWallet.Id,
            RelatedWalletId = charlieUsdWallet.Id,
            Reference = "TRF-002-A-" + GenerateId().ToString("N"),
            CreatedAt = now.AddDays(-3),
            IsCompleted = true
        });
        transactions.Add(new()
        {
            Id = GenerateId(),
            Type = TransactionType.TransferIn,
            Amount = 300.00m,
            Description = "Loan repayment from Diana",
            WalletId = charlieUsdWallet.Id,
            RelatedWalletId = dianaUsdWallet.Id,
            Reference = "TRF-002-B-" + GenerateId().ToString("N"),
            CreatedAt = now.AddDays(-3),
            IsCompleted = true
        });

        await context.Transactions.AddRangeAsync(transactions);
        await context.SaveChangesAsync();
    }
}

