# TiMoch.Orleans.TestUtilities

Testing utilities for Microsoft Orleans applications with NUnit integration. Provides base classes, cluster fixtures, and polling utilities for comprehensive Orleans grain testing.

## Features

- **OrleansTestClusterFixture**: Abstract base class for test cluster management with DI support
- **OrleansTestBase**: Base test class with polling utilities and timeout management
- **FluentTestSiloConfigurator**: Easy setup of named memory storage and stream providers
- **NUnitTestContextLoggerProvider**: Integrated logging to NUnit test output (automatically configured)
- **Automatic Configuration**: IConfiguration and logging automatically set up from appsettings.json

## Quick Start

### 1. Create a Test Cluster Fixture

```csharp
[SetUpFixture]
public class MyProjectTestClusterFixture : OrleansTestClusterFixture
{
    protected override ISiloConfigurator CreateSiloConfigurator()
    {
        return new FluentTestSiloConfigurator()
            .WithNamedMemoryStorage("UserStorage")
            .WithNamedMemoryStorage("OrderStorage")
            .WithNamedMemoryStreams("NotificationStream");
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        // IConfiguration is automatically registered and available
        // NUnit logging is automatically configured
        
        // Register real services
        services.AddSingleton<IMyService, MyService>();
        
        // Override with test doubles
        services.AddSingleton<IExternalApi, MockExternalApi>();
    }
}
```

### 2. Create Test Classes

```csharp
public class UserGrainTests : OrleansTestBase
{
    [Test]
    public async Task UserGrain_Should_UpdateStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userGrain = GrainFactory.GetGrain<IUserGrain>(userId);

        // Act
        await userGrain.SetStatus("Active");

        // Assert - using polling to wait for eventual consistency
        var finalStatus = await PollForConditionAsync(
            async () => await userGrain.GetStatus(),
            status => status == "Active",
            timeout: TimeSpan.FromSeconds(5));

        Assert.That(finalStatus, Is.EqualTo("Active"));
    }

    [Test]
    public async Task UserGrain_Should_ProcessAsync()
    {
        // Arrange
        var userGrain = GrainFactory.GetGrain<IUserGrain>(Guid.NewGuid());

        // Act
        await userGrain.StartProcessing();

        // Assert - simple boolean condition polling
        await PollForConditionAsync(
            async () => await userGrain.IsProcessingComplete(),
            timeout: TimeSpan.FromSeconds(10));
    }
}
```

## Polling Utilities

The `OrleansTestBase` provides powerful polling utilities for testing eventually consistent Orleans systems:

### PollForConditionAsync&lt;T&gt;

Polls for a condition on a returned value:

```csharp
var result = await PollForConditionAsync(
    async () => await grain.GetCount(),
    count => count >= 5,
    timeout: TimeSpan.FromSeconds(10),
    pollInterval: TimeSpan.FromMilliseconds(100));
```

### PollForConditionAsync (Boolean)

Polls for a simple boolean condition:

```csharp
await PollForConditionAsync(
    async () => await grain.IsReady(),
    timeout: TimeSpan.FromSeconds(5));
```

## Automatic Features

### Configuration Support
- `IConfiguration` automatically loaded from `appsettings.json` in base directory
- Available for injection in all Orleans grains and services
- Override `CreateConfiguration()` method to customize configuration sources

### NUnit Logging Integration
- Orleans logs automatically appear in NUnit test output
- Configurable via `Logging` section in `appsettings.json`
- Override `ConfigureLogging()` method to customize logging setup

### Debug Support
Tests automatically detect when running under debugger:
- Timeouts extend to 1 hour when debugger attached
- Force debug mode with `DEBUG_TESTS=true` environment variable
- Debug status shown in test output

### Example appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Orleans": "Warning",
      "MyApp": "Debug"
    },
    "NUnitTestContext": {
      "LogLevel": {
        "Default": "Information"
      }
    }
  }
}
```

## Requirements

- .NET 8.0
- Microsoft Orleans 9.1.2+
- NUnit 3.14.0+

## License

MIT License - see [GitHub repository](https://github.com/timoch/timoch-orleans-testutilities) for details.