namespace Cirreum.Persistence.Configuration;

using Microsoft.Azure.Cosmos;

internal class AzureCosmosItemConfiguration(
	Type type,
	string containerName,
	string partitionKeyPath,
	UniqueKeyPolicy? uniqueKeyPolicy) {

	public Type Type { get; } = type;

	public string ContainerName { get; } = containerName;

	public string PartitionKeyPath { get; } = partitionKeyPath;

	public UniqueKeyPolicy? UniqueKeyPolicy { get; } = uniqueKeyPolicy;

}