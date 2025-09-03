using Orleans.Hosting;
using Orleans.TestingHost;

namespace TiMoch.Orleans.TestUtilities;

/// <summary>
/// Fluent API for configuring Orleans test silos with memory providers.
/// Provides an easy way to set up named memory storage and stream providers for testing.
/// </summary>
public class FluentTestSiloConfigurator : ISiloConfigurator
{
    private readonly List<string> memoryStorageNames = new();
    private readonly List<string> memoryStreamNames = new();

    /// <summary>
    /// Adds a named memory grain storage provider.
    /// </summary>
    /// <param name="name">Name of the storage provider</param>
    /// <returns>This configurator for method chaining</returns>
    public FluentTestSiloConfigurator WithNamedMemoryStorage(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Storage name cannot be null or empty", nameof(name));
            
        memoryStorageNames.Add(name);
        return this;
    }

    /// <summary>
    /// Adds a named memory stream provider.
    /// </summary>
    /// <param name="name">Name of the stream provider</param>
    /// <returns>This configurator for method chaining</returns>
    public FluentTestSiloConfigurator WithNamedMemoryStreams(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Stream provider name cannot be null or empty", nameof(name));
            
        memoryStreamNames.Add(name);
        return this;
    }

    /// <summary>
    /// Configures the silo with all specified memory providers.
    /// </summary>
    /// <param name="siloBuilder">The silo builder to configure</param>
    public void Configure(ISiloBuilder siloBuilder)
    {
        // Add all named memory storage providers
        foreach (var name in memoryStorageNames)
        {
            siloBuilder.AddMemoryGrainStorage(name);
        }

        // Add all named memory stream providers
        foreach (var name in memoryStreamNames)
        {
            siloBuilder.AddMemoryStreams(name);
        }
    }
}