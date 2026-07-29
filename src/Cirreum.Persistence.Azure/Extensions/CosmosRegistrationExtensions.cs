namespace Cirreum.Persistence.Extensions;

using Azure.Core;
using Cirreum.Authorization.Resources;
using Cirreum.Persistence;
using Cirreum.Persistence.Configuration;
using Cirreum.Persistence.Health;
using Cirreum.Persistence.Internal;
using Cirreum.Persistence.Internal.Providers;
using Cirreum.Providers.Configuration;
using Cirreum.ServiceProvider.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

internal static class CosmosRegistrationExtensions {

	public static void AddRepositories(
		this IServiceCollection services,
		string serviceKey,
		AzureCosmosInstanceSettings settings) {

		// Fail at registration, not first use: data-plane RBAC cannot create databases or
		// containers, so auto-creation under identity auth would 403 at the first missing
		// resource — at runtime, after startup looked healthy.
		if (settings.AccountEndpoint is not null && settings.IsAutoResourceCreationEnabled) {
			throw new NotSupportedException(
				"Identity-based authentication with IsAutoResourceCreationEnabled is not a supported configuration: " +
				"Cosmos DB data-plane RBAC cannot create databases or containers. Set " +
				"\"IsAutoResourceCreationEnabled\": false and provision resources as infrastructure-as-code.");
		}

		if (settings.AccountEndpoint is null && settings.Credential is not null) {
			throw new InvalidOperationException(
				"A Credential block is configured but the connection value is a key-based connection string. " +
				"Identity-based authentication requires the account endpoint URI as the connection value.");
		}

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
		services.Add(ServiceDescriptor.DescribeKeyed(typeof(IProtectedRepository<>), serviceKey, typeof(DefaultProtectedRepository<>), lifetime));
		services.Add(ServiceDescriptor.DescribeKeyed(typeof(IAccessEntryProvider<>), serviceKey, typeof(DefaultAccessEntryProvider<>), lifetime));

		// Register Default (non-Keyed) Repositories (will create its own unique instance)
		if (serviceKey.Equals(ServiceProviderSettings.DefaultKey, StringComparison.OrdinalIgnoreCase)) {
			services.Add(ServiceDescriptor.Describe(typeof(IBatchRepository<>), typeof(DefaultRepository<>), lifetime));
			services.Add(ServiceDescriptor.Describe(typeof(IReadOnlyRepository<>), typeof(DefaultRepository<>), lifetime));
			services.Add(ServiceDescriptor.Describe(typeof(IWriteOnlyRepository<>), typeof(DefaultRepository<>), lifetime));
			services.Add(ServiceDescriptor.Describe(typeof(IRepository<>), typeof(DefaultRepository<>), lifetime));
			services.Add(ServiceDescriptor.Describe(typeof(IProtectedRepository<>), typeof(DefaultProtectedRepository<>), lifetime));
			services.Add(ServiceDescriptor.Describe(typeof(IAccessEntryProvider<>), typeof(DefaultAccessEntryProvider<>), lifetime));
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

		var version = typeof(CosmosRegistrationExtensions)
			.Assembly
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
			.InformationalVersion ?? "1.0";
		var cirreumApplicationName = $"cirreum/{version}";

		var clientOptions = settings.EnsureSdkClientOptions();
		clientOptions.ApplicationName =
			string.IsNullOrWhiteSpace(settings.ApplicationName)
				? cirreumApplicationName
				: $"{settings.ApplicationName} {cirreumApplicationName}";
		clientOptions.Serializer = cosmosSystemTextJsonSerializer;
		clientOptions.HttpClientFactory = () => serviceProvider.CreateCosmosHttpClient();
		clientOptions.EnableContentResponseOnWrite = false;
		clientOptions.AllowBulkExecution = settings.AllowBulkExecution;

		// Needs to be enabled for either logging or tracing to work.
		clientOptions.CosmosClientTelemetryOptions.DisableDistributedTracing = false;

		return new DefaultCosmosClientProvider(
			settings.AccountEndpoint is not null ?
			new CosmosClient(settings.AccountEndpoint.OriginalString, settings.GetCredential(), clientOptions) :
			new CosmosClient(settings.ConnectionString, clientOptions));
	}

	private static TokenCredential GetCredential(
		this AzureCosmosInstanceSettings settings) {

		var tenantId = string.IsNullOrWhiteSpace(settings.Identifier) ? null : settings.Identifier;
		var credential = settings.Credential ?? new CredentialSettings();
		var identityId = string.IsNullOrWhiteSpace(credential.IdentityId) ? null : credential.IdentityId;

		return credential.Mode switch {

			CredentialMode.Default => new DefaultAzureCredential(new DefaultAzureCredentialOptions {
				TenantId = tenantId,
				ManagedIdentityClientId = identityId,
			}),

			CredentialMode.ManagedIdentity => new ManagedIdentityCredential(
				identityId is null
					? ManagedIdentityId.SystemAssigned
					: ManagedIdentityId.FromUserAssignedClientId(identityId)),

			CredentialMode.Developer => new ChainedTokenCredential(
				new VisualStudioCredential(new VisualStudioCredentialOptions { TenantId = tenantId }),
				new AzureCliCredential(new AzureCliCredentialOptions { TenantId = tenantId }),
				new AzurePowerShellCredential(new AzurePowerShellCredentialOptions { TenantId = tenantId })),

			_ => throw new InvalidOperationException(
				$"CredentialMode '{credential.Mode}' is not supported by the Azure Cosmos DB provider."),

		};

	}

	private static HttpClient CreateCosmosHttpClient(this IServiceProvider serviceProvider) {

		// A named client, so an application can shape the handler under Cosmos traffic on its own.
		// The unnamed client left ConfigureHttpClientDefaults as the only lever, and that reaches
		// every default client in the application rather than just this one.
		var client = serviceProvider
			.GetRequiredService<IHttpClientFactory>()
			.CreateClient(AzureCosmosDefaults.HttpClientName);

		return client;

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