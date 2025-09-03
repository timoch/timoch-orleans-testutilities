# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview
TiMoch.Orleans.TestUtilities is a NuGet package providing testing utilities for Microsoft Orleans applications. It offers abstract base classes, test cluster management, polling utilities, and NUnit integration for Orleans grain testing.

## Common Development Commands

### Build and Test
```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Build in Release configuration
dotnet build --configuration Release

# Test (if tests exist)
dotnet test
```

### Package Management
```bash
# Create NuGet package
dotnet pack src/TiMoch.Orleans.TestUtilities/TiMoch.Orleans.TestUtilities.csproj --configuration Release --output artifacts/nuget

# Test package build without publishing
./scripts/publish-nuget.sh --dry-run

# Version bump (patch/minor/major)
./scripts/bump-version.sh patch --dry-run
```

## Architecture

### Core Components
- **OrleansTestClusterFixture**: Abstract base class using template method pattern for test cluster management. Provides shared TestCluster instance, configuration methods, and NUnit integration.
- **OrleansTestBase**: Generic test base class with polling utilities and debug-aware timeouts. Manages test execution context and provides convenient grain access.
- **CompositeSiloConfigurator**: Internal composite pattern implementation combining silo configuration with DI service setup.
- **FluentTestSiloConfigurator**: Fluent API for memory storage provider configuration.
- **NUnitTestContextLoggerProvider**: Custom logger provider for routing Orleans logs to NUnit test output.

### Template Method Pattern
The `OrleansTestClusterFixture` uses template method pattern:
- `CreateSiloConfigurator()` - Abstract method for Orleans silo setup
- `ConfigureServices()` - Virtual method for DI container setup  
- `ConfigureLogging()` - Virtual method for logging configuration
- `CreateConfiguration()` - Virtual method for configuration setup

### Project Structure
```
src/TiMoch.Orleans.TestUtilities/
├── OrleansTestClusterFixture.cs    # Main test cluster management
├── OrleansTestBase.cs              # Base class for test cases
├── CompositeSiloConfigurator.cs    # Internal composite configurator
├── FluentTestSiloConfigurator.cs   # Fluent configuration API
└── NUnitTestContextLoggerProvider.cs # NUnit logging integration
```

## Key Features
- **Generic Orleans Test Infrastructure**: Reusable abstract base classes for Orleans testing
- **Debug-Aware Timeouts**: Automatically extends timeouts when debugger attached (1 hour vs 5 seconds)
- **Polling Utilities**: Built-in methods for testing eventual consistency patterns
- **Template Method Pattern**: Extensible configuration through abstract/virtual methods
- **Professional Logging**: Test context-aware logging with configurable levels

## Dependencies
- Microsoft.Orleans.TestingHost (9.1.2)
- NUnit (3.14.0)
- Microsoft.Extensions.* packages for DI, Configuration, and Logging
- Target Framework: .NET 8.0

## Usage Pattern
1. Create test cluster fixture with `[SetUpFixture]` inheriting `OrleansTestClusterFixture`
2. Implement abstract `CreateSiloConfigurator()` method
3. Create test base class inheriting `OrleansTestBase`
4. Write test classes inheriting from your test base
5. Use polling utilities for eventual consistency testing

## Publishing Pipeline
- Version management via `./scripts/bump-version.sh`
- NuGet publishing via `./scripts/publish-nuget.sh`
- GitHub Actions CI/CD pipeline triggered by version tags
- Automated publishing to nuget.org on tagged releases