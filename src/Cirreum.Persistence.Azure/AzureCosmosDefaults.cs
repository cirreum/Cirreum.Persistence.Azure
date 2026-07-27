namespace Cirreum.Persistence;

/// <summary>
/// Default values for the Azure Cosmos DB persistence provider.
/// </summary>
public static class AzureCosmosDefaults {

	/// <summary>
	/// The name of the <see cref="System.Net.Http.IHttpClientFactory"/> client used for Cosmos
	/// gateway traffic.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Cosmos traffic previously went through the factory's <em>unnamed</em> client, so the only way
	/// to shape the handler underneath it was <c>ConfigureHttpClientDefaults</c> — which reaches every
	/// default client in the application, not just Cosmos. Naming it gives Cosmos its own seam:
	/// </para>
	/// <code>
	/// builder.Services.AddHttpClient(AzureCosmosDefaults.HttpClientName)
	///     .ConfigurePrimaryHttpMessageHandler(() =&gt; new SocketsHttpHandler {
	///         PooledConnectionLifetime = TimeSpan.FromSeconds(30),
	///         PooledConnectionIdleTimeout = TimeSpan.FromSeconds(15),
	///         ConnectTimeout = TimeSpan.FromSeconds(2)
	///     });
	/// </code>
	/// <para>
	/// The framework names this client but deliberately does <strong>not</strong> configure it. The
	/// right handler values are environment-specific — aggressive connection recycling suits a local
	/// emulator behind a proxy and causes needless churn against real Azure — and
	/// <c>ConfigurePrimaryHttpMessageHandler</c> is last-write-wins, so a framework-supplied handler
	/// would start an ordering fight with the application's own configuration. Naming without
	/// configuring leaves stock defaults in place unless the application says otherwise.
	/// </para>
	/// <para>
	/// This applies to Gateway connection mode. In Direct mode the SDK uses its own TCP stack for data
	/// -plane operations and this client carries only metadata traffic.
	/// </para>
	/// </remarks>
	public const string HttpClientName = "Cirreum.Cosmos";

}
