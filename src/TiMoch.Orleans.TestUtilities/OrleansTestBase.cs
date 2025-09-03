using System.Diagnostics;
using NUnit.Framework;
using Orleans;

namespace TiMoch.Orleans.TestUtilities;

/// <summary>
/// Abstract base class for Orleans grain tests.
/// Provides polling utilities, timeout management, and test lifecycle support.
/// </summary>
[TestFixture]
public abstract class OrleansTestBase
{
    /// <summary>
    /// Gets the grain factory from the test cluster fixture.
    /// </summary>
    protected static IGrainFactory GrainFactory => OrleansTestClusterFixture.GrainFactory;
    
    /// <summary>
    /// Gets the test timeout. Automatically extends to 1 hour when debugger is attached.
    /// </summary>
    protected TimeSpan TestTimeout => IsDebugging ? TimeSpan.FromHours(1) : TimeSpan.FromSeconds(5);
    
    /// <summary>
    /// Gets the poll interval for checking test conditions.
    /// </summary>
    protected TimeSpan PollInterval => TimeSpan.FromMilliseconds(50);
    
    /// <summary>
    /// Checks if we're in debug mode (timeouts disabled).
    /// Automatically detected when debugger is attached, or can be forced via DEBUG_TESTS=true.
    /// </summary>
    protected bool IsDebugging => Debugger.IsAttached || 
                                   Environment.GetEnvironmentVariable("DEBUG_TESTS") == "true";

    /// <summary>
    /// Sets up the test execution context for proper log routing.
    /// </summary>
    [SetUp]
    public virtual void SetUp()
    {
        // Set the current test execution context for proper log routing
        OrleansTestClusterFixture.CurrentTestExecutionContext = NUnit.Framework.Internal.TestExecutionContext.CurrentContext;
        
        if (IsDebugging)
        {
            TestContext.WriteLine("🔍 Debug mode enabled - timeouts disabled");
        }
    }
    
    /// <summary>
    /// Clears the test execution context.
    /// </summary>
    [TearDown]
    public virtual void TearDown()
    {
        // Clear the current test execution context
        OrleansTestClusterFixture.CurrentTestExecutionContext = null;
    }

    /// <summary>
    /// Polls for a condition to become true, with timeout protection.
    /// Returns the final value when the condition is met.
    /// </summary>
    /// <typeparam name="T">Type of value being polled</typeparam>
    /// <param name="getter">Function that gets the current value</param>
    /// <param name="condition">Condition that must be satisfied</param>
    /// <param name="timeout">Optional timeout override (defaults to TestTimeout)</param>
    /// <param name="pollInterval">Optional poll interval override (defaults to PollInterval)</param>
    /// <param name="timeoutMessage">Custom message for timeout failures</param>
    /// <returns>The final value when condition is met</returns>
    protected async Task<T> PollForConditionAsync<T>(
        Func<Task<T>> getter,
        Func<T, bool> condition,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        string timeoutMessage = "Condition not met within timeout")
    {
        var actualTimeout = timeout ?? TestTimeout;
        var actualPollInterval = pollInterval ?? PollInterval;
        var start = DateTime.UtcNow;
        T lastValue = default!;
        
        while (DateTime.UtcNow - start < actualTimeout)
        {
            lastValue = await getter();
            
            if (condition(lastValue))
                return lastValue;
                
            await Task.Delay(actualPollInterval);
        }
        
        Assert.Fail($"{timeoutMessage}. Timeout: {actualTimeout}. Last value: {lastValue}");
        return lastValue; // Never reached
    }

    /// <summary>
    /// Polls for a simple boolean condition to become true, with timeout protection.
    /// </summary>
    /// <param name="condition">Boolean condition that must become true</param>
    /// <param name="timeout">Optional timeout override (defaults to TestTimeout)</param>
    /// <param name="pollInterval">Optional poll interval override (defaults to PollInterval)</param>
    /// <param name="timeoutMessage">Custom message for timeout failures</param>
    protected async Task PollForConditionAsync(
        Func<Task<bool>> condition,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        string timeoutMessage = "Condition not met within timeout")
    {
        await PollForConditionAsync(
            condition,
            result => result,
            timeout,
            pollInterval,
            timeoutMessage);
    }
}