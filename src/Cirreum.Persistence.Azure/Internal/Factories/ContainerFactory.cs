namespace Cirreum.Persistence.Internal.Factories;

using Cirreum.Persistence;
using Cirreum.Persistence.Extensions;
using Cirreum.Persistence.Internal;
using Cirreum.Persistence.Internal.Providers;
using Cirreum.Persistence.Internal.Resolvers;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

/// <summary>
/// Get or Create Cosmos Containers
/// </summary>
/// <remarks>
/// Resolution is cached per service key. It is not a per-operation concern: every repository call
/// begins by asking for its container, and resolving it means two Cosmos metadata round trips —
/// <c>CreateDatabaseIfNotExistsAsync</c> and <c>CreateContainerIfNotExistsAsync</c>, each of which
/// reads before it creates. Uncached, that doubled the network cost of every read and write for the
/// life of the process, and while the database and container did not yet exist each of those reads
/// returned 404 — so seeding a fresh service produced a stream of expected not-founds in logs and
/// telemetry, one pair per item written.
/// </remarks>
internal sealed class ContainerFactory<TEntity>(
	IServiceProvider serviceProvider,
	ILogger<ContainerFactory<TEntity>> logger)
	: IContainerFactory<TEntity>
	where TEntity : IEntity {

	private static readonly ContainerProperties _containerProperties;

	// Lazy rather than the task directly: ConcurrentDictionary may invoke a GetOrAdd factory more
	// than once under contention, and each invocation here is a pair of round trips and a possible
	// resource creation. ExecutionAndPublication collapses a burst of first callers onto one.
	private readonly ConcurrentDictionary<string, Lazy<Task<Container>>> _containers =
		new(StringComparer.Ordinal);

	// Static constructor to set up the configuration once per type
	static ContainerFactory() {
		_containerProperties = GetContainerProperties();
	}
	private static ContainerProperties GetContainerProperties() {

		var itemType = typeof(TEntity);
		itemType.IsItem();

		var containerName = ContainerNameResolver.GetContainerName(itemType);
		var partitionKeyPath = PartitionKeyPathResolver.GetPartitionKeyPath(itemType);
		var uniqueKeyPolicy = UniqueKeyPolicyResolver.GetUniqueKeyPolicy(itemType);
		var indexingPolicy = IndexingPolicyResolver.GetIndexingPolicy(itemType);

		return new ContainerProperties() {
			Id = containerName,
			PartitionKeyPath = partitionKeyPath,
			UniqueKeyPolicy = uniqueKeyPolicy ?? new(),
			IndexingPolicy = indexingPolicy ?? new(),
			PartitionKeyDefinitionVersion = PartitionKeyDefinitionVersion.V2
		};

	}

	public Task<Container> GetContainerAsync(string key) =>
		this._containers.GetOrAdd(
			key,
			k => new Lazy<Task<Container>>(
				() => this.ResolveContainerAsync(k),
				LazyThreadSafetyMode.ExecutionAndPublication)).Value;

	private async Task<Container> ResolveContainerAsync(string key) {

		try {

			var settings = InstanceSettingsRegistry.GetSettings(key);
			var provider = serviceProvider.GetRequiredKeyedService<ICosmosClientProvider>(key);
			var database =
				settings.IsAutoResourceCreationEnabled
					? await provider.UseClientAsync(
						client => client
							.CreateDatabaseIfNotExistsAsync(settings.DatabaseId))
							.ConfigureAwait(false)
					: await provider.UseClientAsync(
						client => Task
							.FromResult(client.GetDatabase(settings.DatabaseId)))
							.ConfigureAwait(false);

			var container =
				settings.IsAutoResourceCreationEnabled
					? await database
						.CreateContainerIfNotExistsAsync(_containerProperties)
						.ConfigureAwait(false)
					: await Task
						.FromResult(database.GetContainer(_containerProperties.Id))
						.ConfigureAwait(false);

			return container;

		} catch (Exception ex) {
			// Evict before rethrowing: a cached faulted task would make one transient failure during
			// startup permanent for the process, turning a retryable blip into an outage.
			this._containers.TryRemove(key, out _);
			logger.LogError(ex, "Failed to get container with error {GetContainerError}", ex.Message);
			throw;
		}

	}

}
