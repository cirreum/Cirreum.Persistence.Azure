namespace Cirreum.Persistence.Internal.Factories;

using Cirreum.Persistence;
using Cirreum.Persistence.Extensions;
using Cirreum.Persistence.Internal;
using Cirreum.Persistence.Internal.Providers;
using Cirreum.Persistence.Internal.Resolvers;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Get or Create Cosmos Containers
/// </summary>
internal sealed class ContainerFactory<TEntity>(
	IServiceProvider serviceProvider,
	ILogger<ContainerFactory<TEntity>> logger)
	: IContainerFactory<TEntity>
	where TEntity : IEntity {

	private static readonly ContainerProperties _containerProperties;

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

		return new ContainerProperties() {
			Id = containerName,
			PartitionKeyPath = partitionKeyPath,
			UniqueKeyPolicy = uniqueKeyPolicy ?? new(),
			PartitionKeyDefinitionVersion = PartitionKeyDefinitionVersion.V2
		};

	}
	public async Task<Container> GetContainerAsync(string key) {

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
			logger.LogError(ex, "Failed to get container with error {GetContainerError}", ex.Message);
			throw;
		}

	}

}