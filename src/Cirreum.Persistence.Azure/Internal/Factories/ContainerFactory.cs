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
using System.Net;

/// <summary>
/// Resolves the Cosmos container associated with an entity type and service key.
/// </summary>
/// <remarks>
/// <para>
/// Container resolution is cached for the lifetime of the factory, once per service key.
/// The factory itself is registered as a singleton for each closed entity type.
/// </para>
/// <para>
/// When automatic resource creation is enabled, caching prevents repeated Cosmos metadata
/// operations through <c>CreateDatabaseIfNotExistsAsync</c> and
/// <c>CreateContainerIfNotExistsAsync</c>. When automatic creation is disabled, caching
/// retains the equivalent lightweight SDK container handle and provides consistent behavior
/// across both configuration modes.
/// </para>
/// <para>
/// Failed initialization attempts are not retained. A later call may retry resolution after
/// a transient failure.
/// </para>
/// </remarks>
internal sealed class ContainerFactory<TEntity>(
	IServiceProvider serviceProvider,
	ILogger<ContainerFactory<TEntity>> logger
) : IContainerFactory<TEntity>
	where TEntity : IEntity {

	private static readonly ContainerProperties ContainerProperties =
		GetContainerProperties();

	private readonly ConcurrentDictionary<string, Lazy<Task<Container>>> _containers =
		new(StringComparer.Ordinal);

	public Task<Container> GetContainerAsync(string key) {

		ArgumentException.ThrowIfNullOrWhiteSpace(key);

		var lazy = this._containers.GetOrAdd(
			key,
			k => new Lazy<Task<Container>>(
				() => this.ResolveContainerAsync(k),
				LazyThreadSafetyMode.ExecutionAndPublication));

		// Every repository operation begins here. After successful initialization, return
		// the cached task directly. Incomplete, canceled, or faulted initialization is
		// awaited through the slower path so a failed cache entry can be removed.
		var resolution = lazy.Value;

		return resolution.IsCompletedSuccessfully
			? resolution
			: this.GetContainerCoreAsync(key, lazy);

	}

	private async Task<Container> GetContainerCoreAsync(
		string key,
		Lazy<Task<Container>> lazy) {

		try {
			return await lazy.Value.ConfigureAwait(false);
		} catch {

			// Do not retain a faulted initialization task. Remove the entry only
			// if it still contains the same Lazy instance that failed.
			this._containers.TryRemove(
				new KeyValuePair<string, Lazy<Task<Container>>>(key, lazy));

			throw;

		}

	}

	private async Task<Container> ResolveContainerAsync(string key) {

		var settings = InstanceSettingsRegistry.GetSettings(key);
		var entityName = typeof(TEntity).FullName;

		try {

			var provider =
				serviceProvider.GetRequiredKeyedService<ICosmosClientProvider>(key);

			if (!settings.IsAutoResourceCreationEnabled) {
				return provider
					.UseClient(client => client.GetDatabase(settings.DatabaseId))
					.GetContainer(ContainerProperties.Id);
			}

			// Typed rather than var: the call returns DatabaseResponse, and only the implicit
			// conversion to Database exposes CreateContainerIfNotExistsAsync.
			Database database = await provider
				.UseClientAsync(client => client.CreateDatabaseIfNotExistsAsync(settings.DatabaseId))
				.ConfigureAwait(false);

			var response = await database
				.CreateContainerIfNotExistsAsync(ContainerProperties)
				.ConfigureAwait(false);

			// The response distinguishes 201 Created from 200 OK, which is the question a
			// bootstrap run is actually asking — "ensured it exists" answers it for nothing.
			// Reading it here is the only chance: the implicit conversion to Container below
			// discards the status.
			if (logger.IsEnabled(LogLevel.Information)) {
				logger.LogInformation(
					"Cosmos container {ContainerId} {Outcome} in database {DatabaseId} " +
					"for entity {EntityType} using service key {ServiceKey}.",
					ContainerProperties.Id,
					response.StatusCode == HttpStatusCode.Created ? "created" : "already existed",
					settings.DatabaseId,
					entityName,
					key);
			}

			return response;

		} catch (Exception ex) {

			logger.LogError(
				ex,
				"Failed to resolve Cosmos container {ContainerId} in database {DatabaseId} " +
				"for entity {EntityType} using service key {ServiceKey}. " +
				"Automatic resource creation enabled: {AutoCreationEnabled}.",
				ContainerProperties.Id,
				settings.DatabaseId,
				entityName,
				key,
				settings.IsAutoResourceCreationEnabled);

			throw;

		}

	}

	private static ContainerProperties GetContainerProperties() {

		var itemType = typeof(TEntity);
		itemType.EnsureIsItemType();

		var containerName =
			ContainerNameResolver.GetContainerName(itemType);

		var partitionKeyPath =
			PartitionKeyPathResolver.GetPartitionKeyPath(itemType);

		var uniqueKeyPolicy =
			UniqueKeyPolicyResolver.GetUniqueKeyPolicy(itemType);

		var indexingPolicy =
			IndexingPolicyResolver.GetIndexingPolicy(itemType);

		return new ContainerProperties {
			Id = containerName,
			PartitionKeyPath = partitionKeyPath,
			UniqueKeyPolicy = uniqueKeyPolicy ?? new(),
			IndexingPolicy = indexingPolicy ?? new(),
			PartitionKeyDefinitionVersion = PartitionKeyDefinitionVersion.V2
		};

	}

}