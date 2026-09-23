using WalletSystem.Demo.Client.Core;
using WalletSystem.Demo.Shared.Dtos;

namespace WalletSystem.Demo.Client.Strategies;

/// <summary>
/// Strategy interface for test execution
/// Applied Strategy Pattern - defines interchangeable test algorithms
/// </summary>
public interface ITestStrategy
{
    string Name { get; }
    Task<TestSuiteResult> ExecuteAsync(TestContext context, CancellationToken cancellationToken = default);
}
