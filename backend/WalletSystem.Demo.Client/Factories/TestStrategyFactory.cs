using WalletSystem.Demo.Client.Strategies;

namespace WalletSystem.Demo.Client.Factories;

/// <summary>
/// Factory for creating test strategies
/// Applied Factory Method Pattern - centralizes strategy creation
/// </summary>
public static class TestStrategyFactory
{
    /// <summary>
    /// Creates a test strategy by name
    /// </summary>
    public static ITestStrategy Create(string strategyName)
    {
        return strategyName.ToLower() switch
        {
            "basic" or "basicoperations" => new BasicOperationsTestStrategy(),
            "concurrency" => new ConcurrencyTestStrategy(),
            "stress" => new StressTestStrategy(),
            _ => throw new ArgumentException($"Unknown test strategy: {strategyName}", nameof(strategyName))
        };
    }

    /// <summary>
    /// Creates all available test strategies
    /// </summary>
    public static IReadOnlyList<ITestStrategy> CreateAll()
    {
        return new List<ITestStrategy>
        {
            new BasicOperationsTestStrategy(),
            new ConcurrencyTestStrategy(),
            new StressTestStrategy()
        };
    }

    /// <summary>
    /// Gets all available strategy names
    /// </summary>
    public static IReadOnlyList<string> GetAvailableStrategies()
    {
        return new List<string> { "BasicOperations", "Concurrency", "Stress" };
    }
}
