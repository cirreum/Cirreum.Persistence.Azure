namespace Cirreum.Persistence.Internal.Providers;

using Microsoft.Azure.Cosmos;

/// <summary>
/// The cosmos client provider exposes a means of providing
/// an instance to the configured <see cref="CosmosClient"/> object,
/// which is shared.
/// </summary>
interface ICosmosClientProvider {
	Task<T> UseClientAsync<T>(Func<CosmosClient, Task<T>> consume);
}
