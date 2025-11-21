namespace Cirreum.Persistence;

using Cirreum.Persistence.Configuration;
using Cirreum.Persistence.Extensions;
using Cirreum.Persistence.Health;
using Cirreum.Persistence.Internal.Factories;
using Cirreum.Persistence.Internal.Processors;
using Cirreum.ServiceProvider;
using Cirreum.ServiceProvider.Health;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

/// <summary>
/// Registrar responsible for auto-registering any configured messaging clients for the
/// 'Azure' Service Providers in the Persistence section of application settings.
/// </summary>
public sealed class AzureCosmosRegistrar() :
	ServiceProviderRegistrar<
		AzureCosmosSettings,
		AzureCosmosInstanceSettings,
		AzureCosmosHealthCheckOptions> {

	/// <inheritdoc/>
	public override ProviderType ProviderType { get; } = ProviderType.Persistence;

	/// <inheritdoc/>
	public override string ProviderName { get; } = "Azure";

	/// <inheritdoc/>
	public override string[] ActivitySourceNames { get; } = [
		"Azure.Cosmos.Operation",
		"Azure.Cosmos.Request"
	];

	/// <inheritdoc/>
	public override void Register(
		AzureCosmosSettings providerSettings,
		IServiceCollection services,
		IConfiguration configuration) {

		// Add common registrations...
		services.AddHttpClient();
		services.AddMemoryCache();
		services.AddSingleton(typeof(IContainerFactory<>), typeof(ContainerFactory<>));
		services.AddSingleton<ICosmosQueryableProcessor, DefaultCosmosQueryableProcessor>();

		// Continue with normal registration...
		base.Register(providerSettings, services, configuration);

	}

	/// <inheritdoc/>
	protected override void AddServiceProviderInstance(
		IServiceCollection services,
		string serviceKey,
		AzureCosmosInstanceSettings settings) {
		services.AddRepositories(serviceKey, settings);
	}

	/// <inheritdoc/>
	protected override IServiceProviderHealthCheck<AzureCosmosHealthCheckOptions> CreateHealthCheck(
		IServiceProvider serviceProvider,
		string serviceKey,
		AzureCosmosInstanceSettings settings) {
		return serviceProvider.CreateAzureCosmosHealthCheck(serviceKey, settings);
	}

}