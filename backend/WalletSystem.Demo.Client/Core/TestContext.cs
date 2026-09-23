using System.Collections.Immutable;
using WalletSystem.Application.Contracts.Services.Id;
using WalletSystem.Demo.Shared.Dtos;

namespace WalletSystem.Demo.Client.Core;

/// <summary>
/// Immutable data class representing test execution context
/// Applied Flyweight pattern to share common context data
/// </summary>
public sealed record TestContext(
    string Email,
    string Password,
    Guid WalletId,
    decimal InitialBalance,
    string Currency = "USD",
    int ConcurrencyLevel = 10,
    int RequestCount = 100,
    int DegreeOfParallelism = 20,
    HttpClient? HttpClient = null
);
