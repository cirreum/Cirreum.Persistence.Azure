namespace Cirreum.Persistence.Configuration;

using Cirreum.Persistence.Health;
using Cirreum.ServiceProvider.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;

public sealed class AzureCosmosInstanceSettings
	: ServiceProviderInstanceSettings<AzureCosmosHealthCheckOptions> {

	/// <inheritdoc/>
	public override AzureCosmosHealthCheckOptions? HealthOptions { get; set; }
		= new AzureCosmosHealthCheckOptions();


	/// <summary>
	/// Gets or sets the name of the application. This is associated with the
	/// Cosmos DB Client and used with the <see cref="CosmosClientOptions"/>.
	/// </summary>
	public string ApplicationName { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the cosmos account endpoint <see cref="Uri"/>.
	/// </summary>
	internal Uri? AccountEndpoint { get; set; }

	/// <summary>
	/// Optional client options — a curated, configuration-bindable subset of
	/// <see cref="CosmosClientOptions"/> (e.g. <c>"ClientOptions": { "ConnectionMode": "Gateway" }</c>).
	/// </summary>
	/// <remarks>
	/// Applied onto the underlying <see cref="CosmosClientOptions"/> before the
	/// <c>configureClientOptions</c> callback runs, so code-level configuration always
	/// takes precedence over configuration-bound values.
	/// </remarks>
	public AzureCosmosClientSettings? ClientOptions { get; set; }

	/// <summary>
	/// The underlying SDK client options.
	/// </summary>
	internal CosmosClientOptions? SdkClientOptions { get; set; }

	private bool _clientSettingsApplied;

	/// <summary>
	/// Ensures <see cref="SdkClientOptions"/> exists and that <see cref="ClientOptions"/>
	/// has been applied onto it exactly once (so later callers cannot overwrite values
	/// set by the <c>configureClientOptions</c> callback).
	/// </summary>
	internal CosmosClientOptions EnsureSdkClientOptions() {
		this.SdkClientOptions ??= new CosmosClientOptions();
		if (!this._clientSettingsApplied) {
			this.ClientOptions?.ApplyTo(this.SdkClientOptions);
			this._clientSettingsApplied = true;
		}
		return this.SdkClientOptions;
	}

	/// <summary>
	/// Gets or sets the <see cref="ServiceLifetime"/> for <see cref="IRepository{TEntity}"/> instances.
	/// </summary>
	/// <remarks>
	/// The default <see cref="ServiceLifetime"/> is <see cref="ServiceLifetime.Scoped"/>.
	/// Repository instances require access to user context for auditing operations.
	/// Singleton lifetime is not supported due to user context dependencies.
	/// </remarks>
	public ServiceLifetime RepositoryLifetime { get; set; } = ServiceLifetime.Scoped;

	/// <summary>
	/// Gets or sets the name identifier for the cosmos database.
	/// </summary>
	/// <remarks>
	/// Defaults to "cirreum-db", unless otherwise specified.
	/// </remarks>
	public string DatabaseId { get; set; } = "cirreum-db";

	/// <summary>
	/// Gets or sets whether to optimize bandwidth.
	/// When false, the <see cref="ItemRequestOptions.EnableContentResponseOnWrite"/> is set to false and only
	/// headers and status code in the Cosmos DB response for write item operation like Create, Upsert,
	/// Patch and Replace. This reduces networking and CPU load by not sending the resource back over the
	/// network and serializing it on the client.
	/// </summary>
	/// <remarks>
	/// Defaults to <see langword="true"/> - see: <see href="https://devblogs.microsoft.com/cosmosdb/enable-content-response-on-write"/>
	/// </remarks>
	public bool OptimizeBandwidth { get; set; } = true;

	/// <summary>
	/// Gets or sets whether optimistic batching of service requests occurs. Setting this option might
	/// impact the latency of the operations. Hence this option is recommended for non-latency
	/// sensitive scenarios only.
	/// </summary>
	/// <remarks>
	/// Defaults to <see langword="false"/> - see: <see href="https://devblogs.microsoft.com/cosmosdb/introducing-bulk-support-in-the-net-sdk"/>
	/// </remarks>
	public bool AllowBulkExecution { get; set; }

	/// <summary>
	/// Indicate whether or not to try and creates databases and containers if they do not exist.
	/// </summary>
	/// <remarks>
	/// This feature is very powerful for local development. However, in scenarios where
	/// infrastructure as code is used this may not be desired.
	/// </remarks>
	public bool IsAutoResourceCreationEnabled { get; set; } = true;

	/// <summary>
	/// Gets or sets the repository serialization options.
	/// </summary>
	public AzureCosmosRepositorySerializationSettings SerializationOptions { get; } = new();

	/// <summary>
	/// Parses the specified <paramref name="rawValue"/> into either the <see cref="AccountEndpoint"/>
	/// or the <see cref="ServiceProviderInstanceSettings.ConnectionString"/>
	/// </summary>
	/// <param name="rawValue"></param>
	public override void ParseConnectionString(string rawValue) {
		this.ConnectionString = rawValue;
		this.AccountEndpoint = null;
		if (Uri.TryCreate(rawValue, UriKind.Absolute, out var uri)) {
			this.AccountEndpoint = uri;
		}
	}

	/// <inheritdoc/>
	protected override string? ConnectionStringDiscriminator() => this.DatabaseId;

}