using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using NUnit.Framework.Internal;
using Orleans;
using Orleans.TestingHost;

namespace TiMoch.Orleans.TestUtilities;

/// <summary>
/// Abstract base class for Orleans test cluster fixtures.
/// Provides shared test cluster management with configurable silo setup and DI services.
/// Concrete implementations should use [SetUpFixture] attribute and override configuration methods.
/// </summary>
public abstract class OrleansTestClusterFixture
{
    private static TestCluster? cluster;

    /// <summary>
    /// Gets the shared test cluster instance.
    /// </summary>
    protected static TestCluster Cluster => cluster ?? throw new InvalidOperationException("Test cluster not initialized");

    /// <summary>
    /// Gets the grain factory from the shared cluster.
    /// </summary>
    public static IGrainFactory GrainFactory => Cluster.GrainFactory;

    /// <summary>
    /// Gets or sets the current test execution context for routing logs to the correct test output.
    /// Set by test SetUp methods and cleared by TearDown methods.
    /// </summary>
    public static TestExecutionContext? CurrentTestExecutionContext { get; set; }

    /// <summary>
    /// Override this method to configure the Orleans silo for testing.
    /// </summary>
    /// <returns>ISiloConfigurator that defines Orleans infrastructure setup</returns>
    protected abstract ISiloConfigurator CreateSiloConfigurator();

    /// <summary>
    /// Override this method to configure dependency injection services for testing.
    /// Called during silo setup to register application services and test doubles.
    /// The IConfiguration is already registered and available for injection.
    /// NUnit logging is automatically configured - override ConfigureLogging to customize.
    /// </summary>
    /// <param name="services">Service collection to configure</param>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // Default implementation does nothing - override in derived classes
    }

    /// <summary>
    /// Override this method to customize logging configuration for the test cluster.
    /// Default implementation adds NUnit test context logging with configuration support.
    /// </summary>
    /// <param name="builder">Logging builder to configure</param>
    /// <param name="configuration">Configuration instance</param>
    protected virtual void ConfigureLogging(ILoggingBuilder builder, IConfiguration configuration)
    {
        // Read logging configuration from appsettings.json
        builder.AddConfiguration(configuration.GetSection("Logging"));
        
        // Add NUnit test context logger provider with configuration
        builder.AddProvider(new NUnitTestContextLoggerProvider(configuration));
    }

    /// <summary>
    /// Override this method to provide custom configuration for the test cluster.
    /// Default implementation loads from appsettings.json in the base directory.
    /// </summary>
    /// <returns>Configuration instance for the test cluster</returns>
    protected virtual IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .Build();
    }

    /// <summary>
    /// Sets up the test cluster. Called once before all tests in the fixture.
    /// </summary>
    [OneTimeSetUp]
    public async Task SetUpCluster()
    {
        var builder = new TestClusterBuilder();
        
        // Create configuration instance
        var configuration = CreateConfiguration();
        
        // Create a composite configurator that combines silo configuration with DI services
        var compositeSiloConfigurator = new CompositeSiloConfigurator(CreateSiloConfigurator(), ConfigureServices, ConfigureLogging, configuration);
        builder.AddSiloBuilderConfigurator<CompositeSiloConfigurator>();
        
        // Store the configurator instance for the generic method to find
        CompositeSiloConfigurator.Instance = compositeSiloConfigurator;
        
        cluster = builder.Build();
        await cluster.DeployAsync();
    }

    /// <summary>
    /// Tears down the test cluster. Called once after all tests in the fixture.
    /// </summary>
    [OneTimeTearDown]
    public async Task TearDownCluster()
    {
        if (cluster != null)
        {
            await cluster.StopAllSilosAsync();
            cluster.Dispose();
            cluster = null;
        }
    }
}