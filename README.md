# TiMoch.Orleans.TestUtilities

[![NuGet](https://img.shields.io/nuget/v/TiMoch.Orleans.TestUtilities.svg)](https://www.nuget.org/packages/TiMoch.Orleans.TestUtilities)
[![NuGet Downloads](https://img.shields.io/nuget/dt/TiMoch.Orleans.TestUtilities.svg)](https://www.nuget.org/packages/TiMoch.Orleans.TestUtilities)
[![Build Status](https://github.com/timoch/timoch-orleans-testutilities/actions/workflows/nuget-publish.yml/badge.svg)](https://github.com/timoch/timoch-orleans-testutilities/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A comprehensive, reusable Orleans testing infrastructure package that provides generic testing utilities for Microsoft Orleans applications.

## 🚀 Features

- **Generic Orleans Test Infrastructure** - Abstract base classes for Orleans test cluster management
- **Polling Utilities** - Built-in polling methods for eventual consistency testing
- **Debug-Aware Timeouts** - Automatic timeout extension when debugging
- **NUnit Integration** - Seamless integration with NUnit test framework
- **Fluent Configuration** - Easy setup of memory storage providers and test infrastructure
- **Professional Logging** - Test context-aware logging with configurable levels

## 📦 Installation

```bash
dotnet add package TiMoch.Orleans.TestUtilities
```

## 🔧 Quick Start

### 1. Create Test Cluster Fixture

```csharp
using TiMoch.Orleans.TestUtilities;
using Orleans.TestingHost;
using NUnit.Framework;

[SetUpFixture]
public class MyTestClusterFixture : OrleansTestClusterFixture
{
    protected override ISiloConfigurator CreateSiloConfigurator()
    {
        return new MySiloConfigurator();
    }
}

public class MySiloConfigurator : ISiloConfigurator
{
    public void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder.AddMemoryGrainStorage("Default");
        // Add your Orleans services here
    }
}
```

### 2. Create Test Base Class

```csharp
using TiMoch.Orleans.TestUtilities;

public abstract class MyTestBase : OrleansTestBase
{
    protected IMyGrain GetMyGrain(Guid id)
    {
        return GrainFactory.GetGrain<IMyGrain>(id);
    }
}
```

### 3. Write Tests

```csharp
[TestFixture]
public class MyGrainTests : MyTestBase
{
    [Test]
    public async Task MyGrain_ShouldProcessCorrectly()
    {
        // Arrange
        var grain = GetMyGrain(Guid.NewGuid());
        
        // Act
        await grain.DoSomething("test");
        
        // Assert with polling for eventual consistency
        var result = await PollForConditionAsync(
            async () => await grain.GetStatus(),
            status => status == "completed",
            TimeSpan.FromSeconds(5));
            
        Assert.That(result, Is.EqualTo("completed"));
    }
}
```

## 📚 Core Components

### OrleansTestClusterFixture
Abstract base class for Orleans test cluster management using the template method pattern.

**Features:**
- Automatic Orleans test cluster setup and teardown
- Configurable silo and service configuration
- Built-in IConfiguration and logging setup
- NUnit test context integration

### OrleansTestBase  
Generic test base class with polling utilities and debug support.

**Features:**
- Automatic timeout extension when debugger is attached
- Built-in polling methods for eventual consistency
- Access to Orleans GrainFactory
- Test execution context management

### Polling Utilities
Built-in methods for testing eventually consistent Orleans applications:

```csharp
// Poll for a condition to become true
var result = await PollForConditionAsync(
    getter: async () => await grain.GetValue(),
    condition: value => value > 10,
    timeout: TimeSpan.FromSeconds(5));

// Poll with custom intervals
var result = await PollForConditionAsync(
    getter: async () => await grain.GetStatus(),
    condition: status => status.IsComplete,
    timeout: TimeSpan.FromSeconds(10),
    pollInterval: TimeSpan.FromMilliseconds(100));
```

### FluentTestSiloConfigurator
Fluent API for easy memory storage provider setup:

```csharp
public class MySiloConfigurator : ISiloConfigurator
{
    public void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder.AddMemoryGrainStorage("Default")
                   .AddMemoryGrainStorage("UserStorage")
                   .AddMemoryStreams("StreamProvider");
    }
}
```

## 🎯 Advanced Usage

### Custom Polling Logic
```csharp
protected async Task<MyComplexResult> WaitForComplexCondition()
{
    return await PollForConditionAsync(
        async () => {
            var grain1 = GrainFactory.GetGrain<IGrain1>(id1);
            var grain2 = GrainFactory.GetGrain<IGrain2>(id2);
            
            var result1 = await grain1.GetState();
            var result2 = await grain2.GetState();
            
            return new MyComplexResult(result1, result2);
        },
        result => result.IsValid && result.Count > 5,
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMilliseconds(500),
        "Complex condition was not met within timeout");
}
```

### Debug-Aware Testing
The framework automatically detects when you're debugging:

```csharp
// Automatically uses 1 hour timeout when debugger is attached
// Uses 5 second timeout in normal test runs
protected TimeSpan TestTimeout { get; } // Handled automatically

// Check if we're debugging
if (IsDebugging)
{
    TestContext.WriteLine("🔍 Debug mode enabled - timeouts disabled");
}
```

### Service Configuration
```csharp
[SetUpFixture]
public class MyTestClusterFixture : OrleansTestClusterFixture
{
    protected override ISiloConfigurator CreateSiloConfigurator()
    {
        return new MySiloConfigurator();
    }
    
    protected override void ConfigureServices(IServiceCollection services)
    {
        // Add custom services
        services.AddSingleton<IMyService, MyService>();
        
        // Configure options
        services.Configure<MyOptions>(options => {
            options.Setting1 = "test-value";
            options.Setting2 = true;
        });
    }
}
```

## 🔧 Configuration

### Test Timeouts
Control test timeouts through environment variables:

```bash
# Force debug mode (disables timeouts)
export DEBUG_TESTS=true

# Normal test execution uses built-in timeouts
# - 5 seconds default for test operations
# - 50ms polling interval
# - 1 hour when debugger is attached
```

### Logging Configuration
Configure logging through `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Orleans": "Warning",
      "Microsoft": "Warning"
    },
    "NUnitTestContext": {
      "LogLevel": {
        "Default": "Information",
        "Orleans.Runtime": "Debug"
      }
    }
  }
}
```

## 🏗️ Architecture Patterns

### Template Method Pattern
The `OrleansTestClusterFixture` uses the template method pattern:

```csharp
// Framework provides the structure
public abstract class OrleansTestClusterFixture
{
    // Template method - calls abstract methods
    protected virtual void SetUp() 
    {
        var configurator = CreateSiloConfigurator(); // Abstract
        ConfigureServices(services); // Virtual
        // ... setup logic
    }
    
    // Extension points for customization
    protected abstract ISiloConfigurator CreateSiloConfigurator();
    protected virtual void ConfigureServices(IServiceCollection services) { }
}
```

### Composite Pattern
Internal composite configurator combines multiple configuration concerns:

```csharp
// Automatically handled by the framework
// Combines Orleans silo configuration with DI service setup
```

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

### Development Setup
```bash
git clone https://github.com/timoch/timoch-orleans-testutilities.git
cd timoch-orleans-testutilities
dotnet restore
dotnet build
dotnet test
```

### Publishing
```bash
# Bump version
./scripts/bump-version.sh patch

# Test package build
./scripts/publish-nuget.sh --dry-run

# Push to trigger automated publishing
git push && git push --tags
```

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built for the [Microsoft Orleans](https://github.com/dotnet/orleans) community
- Inspired by the need for reusable Orleans testing patterns
- Uses [NUnit](https://nunit.org/) for test framework integration

## 📞 Support

- 🐛 [Report Issues](https://github.com/timoch/timoch-orleans-testutilities/issues)
- 💡 [Feature Requests](https://github.com/timoch/timoch-orleans-testutilities/issues)
- 📖 [Documentation](https://github.com/timoch/timoch-orleans-testutilities/wiki)
- 💬 [Discussions](https://github.com/timoch/timoch-orleans-testutilities/discussions)