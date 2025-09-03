using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans.Hosting;
using Orleans.TestingHost;

namespace TiMoch.Orleans.TestUtilities;

/// <summary>
/// Internal composite silo configurator that combines a base configurator with additional service configuration.
/// This enables the base class pattern where derived classes can override both silo and service configuration.
/// </summary>
internal class CompositeSiloConfigurator : ISiloConfigurator
{
    private readonly ISiloConfigurator baseSiloConfigurator;
    private readonly Action<IServiceCollection> configureServices;
    private readonly Action<ILoggingBuilder, IConfiguration> configureLogging;
    private readonly IConfiguration configuration;

    /// <summary>
    /// Static instance to work around TestClusterBuilder's generic constraint requirements.
    /// </summary>
    public static CompositeSiloConfigurator? Instance { get; set; }

    public CompositeSiloConfigurator()
    {
        // This parameterless constructor is required by TestClusterBuilder.AddSiloBuilderConfigurator<T>()
        // The actual configuration is provided via the static Instance property
        if (Instance == null)
            throw new InvalidOperationException("CompositeSiloConfigurator.Instance must be set before instantiation");
            
        baseSiloConfigurator = Instance.baseSiloConfigurator;
        configureServices = Instance.configureServices;
        configureLogging = Instance.configureLogging;
        configuration = Instance.configuration;
    }

    public CompositeSiloConfigurator(ISiloConfigurator baseSiloConfigurator, Action<IServiceCollection> configureServices, Action<ILoggingBuilder, IConfiguration> configureLogging, IConfiguration configuration)
    {
        this.baseSiloConfigurator = baseSiloConfigurator ?? throw new ArgumentNullException(nameof(baseSiloConfigurator));
        this.configureServices = configureServices ?? throw new ArgumentNullException(nameof(configureServices));
        this.configureLogging = configureLogging ?? throw new ArgumentNullException(nameof(configureLogging));
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public void Configure(ISiloBuilder siloBuilder)
    {
        // Apply base silo configuration first
        baseSiloConfigurator.Configure(siloBuilder);
        
        // Apply additional service configuration
        siloBuilder.ConfigureServices(services =>
        {
            // Register configuration first
            services.AddSingleton<IConfiguration>(configuration);
            
            // Add logging with automatic NUnit integration
            services.AddLogging(builder =>
            {
                configureLogging(builder, configuration);
            });
            
            // Then apply user services
            configureServices(services);
        });
    }
}