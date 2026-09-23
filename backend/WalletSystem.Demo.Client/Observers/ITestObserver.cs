using WalletSystem.Demo.Client.Core;
using WalletSystem.Demo.Shared.Dtos;

namespace WalletSystem.Demo.Client.Observers;

/// <summary>
/// Observer interface for test execution events
/// Applied Observer Pattern - allows multiple observers to react to test events
/// </summary>
public interface ITestObserver
{
    void OnTestStarted(string testName);
    void OnTestCompleted(TestResult result);
    void OnTestSuiteCompleted(TestSuiteResult suiteResult);
}
