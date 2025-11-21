namespace Cirreum.Persistence.Configuration;

using Cirreum.Persistence.Health;
using Cirreum.ServiceProvider.Configuration;

public class AzureCosmosSettings :
	ServiceProviderSettings<
		AzureCosmosInstanceSettings,
		AzureCosmosHealthCheckOptions>;