namespace Cirreum.Persistence.Health;

using Cirreum.Health;

public sealed class AzureCosmosHealthCheckOptions
	 : ServiceProviderHealthCheckOptions {

	/// <summary>
	/// Gets or sets the identifier for the Azure Cosmos database whose health should be checked.
	/// </summary>
	/// <remarks>
	/// If the value is <see langword="null"/>, then no health check is performed for a specific database.
	/// </remarks>
	/// <value>An optional Azure Cosmos database identifier.</value>
	internal string? DatabaseId { get; set; }

	/// <summary>
	/// Gets or sets zero or more identifiers for the Azure Cosmos DB containers
	/// within the database whose health should be checked.
	/// </summary>
	/// <remarks>
	/// If the value is <see langword="null"/>, then no health check is performed for containers.
	/// </remarks>
	/// <value>Zero or more Azure Cosmos DB container identifiers.</value>
	public HashSet<string>? ContainerIds { get; set; }

}