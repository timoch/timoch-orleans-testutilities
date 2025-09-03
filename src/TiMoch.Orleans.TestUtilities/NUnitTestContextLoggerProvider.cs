using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System.Text;

namespace TiMoch.Orleans.TestUtilities;

/// <summary>
/// Logger provider that outputs to NUnit TestContext for better test integration.
/// </summary>
public class NUnitTestContextLoggerProvider : ILoggerProvider
{
    private readonly TestExecutionContext? initialExecutionContext;
    private readonly IConfiguration? configuration;
    private readonly Dictionary<string, LogLevel> logLevelCache = new();

    public NUnitTestContextLoggerProvider(IConfiguration? configuration = null)
    {
        // Capture the initial test execution context at provider creation time
        initialExecutionContext = TestExecutionContext.CurrentContext;
        this.configuration = configuration;
        
        // Pre-cache log levels from configuration if available
        if (configuration != null)
        {
            LoadLogLevels();
        }
    }

    private void LoadLogLevels()
    {
        // Load NUnitTestContext specific log levels
        var nunitSection = configuration?.GetSection("Logging:NUnitTestContext:LogLevel");
        if (nunitSection?.Exists() == true)
        {
            foreach (var kvp in nunitSection.AsEnumerable(true))
            {
                if (!string.IsNullOrEmpty(kvp.Key) && !string.IsNullOrEmpty(kvp.Value) &&
                    Enum.TryParse<LogLevel>(kvp.Value, out var level))
                {
                    logLevelCache[kvp.Key] = level;
                }
            }
        }
        
        // Also load general log levels as fallback
        var generalSection = configuration?.GetSection("Logging:LogLevel");
        if (generalSection?.Exists() == true)
        {
            foreach (var kvp in generalSection.AsEnumerable(true))
            {
                if (!string.IsNullOrEmpty(kvp.Key) && !string.IsNullOrEmpty(kvp.Value) &&
                    !logLevelCache.ContainsKey(kvp.Key) &&
                    Enum.TryParse<LogLevel>(kvp.Value, out var level))
                {
                    logLevelCache[kvp.Key] = level;
                }
            }
        }
    }

    public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
    {
        return new NUnitTestContextLogger(categoryName, initialExecutionContext, logLevelCache);
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}

/// <summary>
/// Logger that writes to NUnit TestContext.WriteLine.
/// </summary>
public class NUnitTestContextLogger : Microsoft.Extensions.Logging.ILogger
{
    private readonly string categoryName;
    private readonly TestExecutionContext? initialExecutionContext;
    private readonly Dictionary<string, LogLevel> logLevelCache;
    private LogLevel? resolvedMinLevel;

    public NUnitTestContextLogger(string categoryName, TestExecutionContext? initialExecutionContext, Dictionary<string, LogLevel> logLevelCache)
    {
        this.categoryName = categoryName;
        this.initialExecutionContext = initialExecutionContext;
        this.logLevelCache = logLevelCache ?? new Dictionary<string, LogLevel>();
        
        // Resolve the minimum log level for this category once
        resolvedMinLevel = ResolveMinimumLogLevel();
    }

    private LogLevel ResolveMinimumLogLevel()
    {
        if (string.IsNullOrEmpty(categoryName))
            return LogLevel.Information;
            
        // Check for exact match
        if (logLevelCache.TryGetValue(categoryName, out var exactLevel))
            return exactLevel;
            
        // Find the longest matching prefix
        string? bestMatch = null;
        LogLevel? bestLevel = null;
        
        foreach (var kvp in logLevelCache)
        {
            if (categoryName.StartsWith(kvp.Key))
            {
                if (bestMatch == null || kvp.Key.Length > bestMatch.Length)
                {
                    bestMatch = kvp.Key;
                    bestLevel = kvp.Value;
                }
            }
        }
        
        if (bestLevel.HasValue)
            return bestLevel.Value;
            
        // Check for Default
        if (logLevelCache.TryGetValue("Default", out var defaultLevel))
            return defaultLevel;
            
        // Final fallback
        return LogLevel.Information;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return NullScope.Instance;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        // Check if test context is available AND log level meets minimum threshold
        return GetActiveContext() != null && logLevel >= resolvedMinLevel;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        try
        {
            var message = new StringBuilder();
            
            // Add timestamp
            message.Append($"[{DateTime.UtcNow:HH:mm:ss.fff}] ");
            
            // Add log level
            message.Append($"[{GetLogLevelString(logLevel)}] ");
            
            // Add category (shortened if too long)
            var shortCategory = GetShortCategoryName(categoryName);
            message.Append($"[{shortCategory}] ");
            
            // Add the actual message
            message.Append(formatter(state, exception));
            
            // Add exception details if present
            if (exception != null)
            {
                message.AppendLine();
                message.Append($"  Exception: {exception}");
            }
            
            // Write to the resolved execution context's output writer
            var context = GetActiveContext();
            context?.OutWriter?.WriteLine(message.ToString());
        }
        catch
        {
            // Ignore any errors writing to TestContext
            // This can happen if the test context becomes invalid
        }
    }
    
    private TestExecutionContext? GetActiveContext()
    {
        // First check if there's a current test execution context set by test fixtures
        var currentContext = OrleansTestClusterFixture.CurrentTestExecutionContext;
        if (currentContext != null)
            return currentContext;
            
        // Fall back to the initial execution context captured at provider creation
        return initialExecutionContext;
    }
    
    private static string GetLogLevelString(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => "TRCE",
            LogLevel.Debug => "DBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERRR",
            LogLevel.Critical => "CRIT",
            LogLevel.None => "NONE",
            _ => "????"
        };
    }
    
    private static string GetShortCategoryName(string categoryName)
    {
        // Shorten long namespace names for readability
        if (categoryName.Length <= 40)
            return categoryName;
            
        // Take last parts of the namespace
        var parts = categoryName.Split('.');
        if (parts.Length >= 2)
        {
            return $"...{parts[^2]}.{parts[^1]}";
        }
        
        return categoryName.Substring(categoryName.Length - 40);
    }
    
    private class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new NullScope();
        
        private NullScope() { }
        
        public void Dispose() { }
    }
}