namespace Cirreum.Persistence.Extensions.Hosting;

using Cirreum.Persistence.Configuration;
using Cirreum.Persistence.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

public static class HostingExtensions {

	/// <summary>
	/// Adds a manually configured Cosmos DB NoSQL connection instance.
	/// </summary>
	/// <param name="builder">The source <see cref="IHostApplicationBuilder"/> to add the service to.</param>
	/// <param name="serviceKey">The DI Service Key (all service providers are registered with a key)</param>
	/// <param name="settings">The configured instance settings.</param>
	/// <param name="configureClientOptions">An optional callback to further edit the client options.</param>
	/// <param name="configureHealthCheckOptions">An optional callback to further edit the health check options.</param>
	/// <returns>The provided <see cref="IServiceCollection"/>.</returns>
	public static IHostApplicationBuilder AddCosmosDb(
		this IHostApplicationBuilder builder,
		string serviceKey,
		AzureCosmosInstanceSettings settings,
		Action<CosmosClientOptions>? configureClientOptions = null,
		Action<AzureCosmosHealthCheckOptions>? configureHealthCheckOptions = null) {

		ArgumentNullException.ThrowIfNull(builder);

		// Ensure minimum services
		builder.Services.AddHttpClient();
		builder.Services.AddMemoryCache();

		// Configure client options
		settings.ClientOptions ??= new CosmosClientOptions();
		configureClientOptions?.Invoke(settings.ClientOptions);

		// Configure health options
		settings.HealthOptions ??= new AzureCosmosHealthCheckOptions();
		configureHealthCheckOptions?.Invoke(settings.HealthOptions);

		// Reuse our Registrar...
		var registrar = new AzureCosmosRegistrar();
		registrar.RegisterInstance(
			serviceKey,
			settings,
			builder.Services,
			builder.Configuration);

		return builder;

	}

	/// <summary>
	/// Adds a manually configured Cosmos DB NoSQL connection instance.
	/// </summary>
	/// <param name="builder">The source <see cref="IHostApplicationBuilder"/> to add the service to.</param>
	/// <param name="serviceKey">The DI Service Key (all service providers are registered with a key)</param>
	/// <param name="configure">The callback to configure the instance settings.</param>
	/// <param name="configureClientOptions">An optional callback to further edit the client options.</param>
	/// <param name="configureHealthCheckOptions">An optional callback to further edit the health check options.</param>
	/// <returns>The provided <see cref="IServiceCollection"/>.</returns>
	public static IHostApplicationBuilder AddCosmosDb(
		this IHostApplicationBuilder builder,
		string serviceKey,
		Action<AzureCosmosInstanceSettings> configure,
		Action<CosmosClientOptions>? configureClientOptions = null,
		Action<AzureCosmosHealthCheckOptions>? configureHealthCheckOptions = null) {

		ArgumentNullException.ThrowIfNull(builder);

		var settings = new AzureCosmosInstanceSettings();
		configure?.Invoke(settings);
		if (string.IsNullOrWhiteSpace(settings.Name)) {
			settings.Name = serviceKey;
		}

		return AddCosmosDb(builder, serviceKey, settings, configureClientOptions, configureHealthCheckOptions);

	}

	/// <summary>
	/// Adds a manually configured Cosmos DB NoSQL connection instance.
	/// </summary>
	/// <param name="builder">The source <see cref="IHostApplicationBuilder"/> to add the service to.</param>
	/// <param name="serviceKey">The DI Service Key (all service providers are registered with a key)</param>
	/// <param name="connectionString">The callback to configure the instance settings.</param>
	/// <param name="configureClientOptions">An optional callback to further edit the client options.</param>
	/// <param name="configureHealthCheckOptions">An optional callback to further edit the health check options.</param>
	/// <returns>The provided <see cref="IServiceCollection"/>.</returns>
	public static IHostApplicationBuilder AddCosmosDb(
		this IHostApplicationBuilder builder,
		string serviceKey,
		string connectionString,
		Action<CosmosClientOptions>? configureClientOptions = null,
		Action<AzureCosmosHealthCheckOptions>? configureHealthCheckOptions = null) {

		ArgumentNullException.ThrowIfNull(builder);

		var settings = new AzureCosmosInstanceSettings() {
			ConnectionString = connectionString,
			Name = serviceKey
		};

		return AddCosmosDb(builder, serviceKey, settings, configureClientOptions, configureHealthCheckOptions);

	}


	/// <summary>
	/// Adds the in-memory implementation of the generic <see cref="IRepository{TEntity}"/>.
	/// </summary>
	/// <param name="services">The service collection to add services to.</param>
	/// <param name="serviceKey">The DI Service Key (all service providers are registered with a key). Default: 'inmemory'</param>
	/// <returns>The source <see cref="IServiceCollection"/></returns>
	public static IServiceCollection AddInMemoryCosmosRepository(
		this IServiceCollection services,
		string serviceKey = "inmemory") {

		ArgumentNullException.ThrowIfNull(services);

		services
			.AddKeyedSingleton(typeof(IBatchRepository<>), serviceKey, typeof(InMemoryRepository<>))
			.AddKeyedSingleton(typeof(IReadOnlyRepository<>), serviceKey, typeof(InMemoryRepository<>))
			.AddKeyedSingleton(typeof(IWriteOnlyRepository<>), serviceKey, typeof(InMemoryRepository<>))
			.AddKeyedSingleton(typeof(IRepository<>), serviceKey, typeof(InMemoryRepository<>));

		return services;

	}

}