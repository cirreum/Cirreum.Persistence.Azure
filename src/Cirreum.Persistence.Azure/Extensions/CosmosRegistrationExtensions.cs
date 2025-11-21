namespace Cirreum.Persistence.Extensions;

using Cirreum.Persistence;
using Cirreum.Persistence.Configuration;
using Cirreum.Persistence.Health;
using Cirreum.Persistence.Internal;
using Cirreum.Persistence.Internal.Providers;
using Cirreum.ServiceProvider.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Data.Common;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

internal static class CosmosRegistrationExtensions {

	public static void AddRepositories(
		this IServiceCollection services,
		string serviceKey,
		AzureCosmosInstanceSettings settings) {

		// Track keyed instance settings
		InstanceSettingsRegistry.RegisterKeyedSettings(serviceKey, settings);

		// Register Keyed Cosmos Client Provider
		services.AddKeyedSingleton<ICosmosClientProvider>(serviceKey,
			(sp, key) => sp.CreateCosmosClientProvider(settings));

		// Resolve and validate DI Lifetime
		var lifetime = settings.RepositoryLifetime;
		if (lifetime == ServiceLifetime.Singleton) {
			throw new InvalidOperationException(
				"Singleton lifetime is not supported for repositories due to user context dependencies. " +
				"Change 'RepositoryLifetime' to 'Scoped' in your configuration. " +
				"Example: \"RepositoryLifetime\": \"Scoped\"");
		}

		// Register Keyed Repositories
		services.Add(ServiceDescriptor.DescribeKeyed(typeof(IBatchRepository<>), serviceKey, typeof(DefaultRepository<>), lifetime));
		services.Add(ServiceDescriptor.DescribeKeyed(typeof(IReadOnlyRepository<>), serviceKey, typeof(DefaultRepository<>), lifetime));
		services.Add(ServiceDescriptor.DescribeKeyed(typeof(IWriteOnlyRepository<>), serviceKey, typeof(DefaultRepository<>), lifetime));
		services.Add(ServiceDescriptor.DescribeKeyed(typeof(IRepository<>), serviceKey, typeof(DefaultRepository<>), lifetime));

		// Register Default (non-Keyed) Repositories (will create its own unique instance)
		if (serviceKey.Equals(ServiceProviderSettings.DefaultKey, StringComparison.OrdinalIgnoreCase)) {
			services.Add(ServiceDescriptor.Describe(typeof(IBatchRepository<>), typeof(DefaultRepository<>), lifetime));
			services.Add(ServiceDescriptor.Describe(typeof(IReadOnlyRepository<>), typeof(DefaultRepository<>), lifetime));
			services.Add(ServiceDescriptor.Describe(typeof(IWriteOnlyRepository<>), typeof(DefaultRepository<>), lifetime));
			services.Add(ServiceDescriptor.Describe(typeof(IRepository<>), typeof(DefaultRepository<>), lifetime));
		}

	}

	private static DefaultCosmosClientProvider CreateCosmosClientProvider(
		this IServiceProvider serviceProvider,
		AzureCosmosInstanceSettings settings) {

		var jsonSerializerOptions = new JsonSerializerOptions() {
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			WriteIndented = settings.SerializationOptions.Indented,
			PropertyNamingPolicy = settings.SerializationOptions.MappedNamingPolicy
		};
		var cosmosSystemTextJsonSerializer = new CosmosSystemTextJsonSerializer(jsonSerializerOptions);

		settings.ClientOptions ??= new();
		settings.ClientOptions.ApplicationName = settings.ApplicationName ?? "Cirreum";
		settings.ClientOptions.Serializer = cosmosSystemTextJsonSerializer;
		settings.ClientOptions.HttpClientFactory = () => serviceProvider.CreateCosmosHttpClient();
		settings.ClientOptions.EnableContentResponseOnWrite = false;
		settings.ClientOptions.AllowBulkExecution = settings.AllowBulkExecution;

		// Needs to be enabled for either logging or tracing to work.
		settings.ClientOptions.CosmosClientTelemetryOptions.DisableDistributedTracing = false;

		if (IsEmulatorConnectionString(settings.ConnectionString)) {
			settings.ClientOptions.ConnectionMode = ConnectionMode.Direct;
			settings.ClientOptions.LimitToEndpoint = true;
		}

		return new DefaultCosmosClientProvider(
			settings.AccountEndpoint is not null ?
			new CosmosClient(settings.AccountEndpoint.OriginalString, new DefaultAzureCredential(), settings.ClientOptions) :
			new CosmosClient(settings.ConnectionString, settings.ClientOptions));
	}
	private static HttpClient CreateCosmosHttpClient(
		this IServiceProvider serviceProvider) {

		var client = serviceProvider
			.GetRequiredService<IHttpClientFactory>()
			.CreateClient();

		var version =
			Assembly.GetExecutingAssembly()
					.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
					.InformationalVersion ?? "1.0";

		client.DefaultRequestHeaders
			  .UserAgent
			  .ParseAdd($"corr/{version}");

		return client;

	}
	private const string EmulatorAccountKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
	private static bool IsEmulatorConnectionString(string? connectionString) {

		if (connectionString == null) {
			return false;
		}

		var builder = new DbConnectionStringBuilder {
			ConnectionString = connectionString
		};
		if (!builder.TryGetValue("AccountKey", out var v)) {
			return false;
		}

		var accountKeyFromConnectionString = v.ToString();

		return accountKeyFromConnectionString == EmulatorAccountKey;

	}


	public static AzureCosmosHealthCheck CreateAzureCosmosHealthCheck(
		this IServiceProvider serviceProvider,
		string serviceKey,
		AzureCosmosInstanceSettings settings) {
		var env = serviceProvider.GetRequiredService<IHostEnvironment>();
		var cache = serviceProvider.GetRequiredService<IMemoryCache>();
		var clientProvider = serviceProvider.GetRequiredKeyedService<ICosmosClientProvider>(serviceKey);
		settings.HealthOptions ??= new AzureCosmosHealthCheckOptions();
		settings.HealthOptions.DatabaseId = settings.DatabaseId;
		return new AzureCosmosHealthCheck(clientProvider, env.IsProduction(), cache, settings.HealthOptions);
	}

}