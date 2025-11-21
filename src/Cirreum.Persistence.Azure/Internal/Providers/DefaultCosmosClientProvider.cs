namespace Cirreum.Persistence.Internal.Providers;

using Microsoft.Azure.Cosmos;

/// <summary>
/// Default implementation of ICosmosClientProvider that wraps a CosmosClient instance.
/// </summary>
sealed class DefaultCosmosClientProvider(CosmosClient client) : ICosmosClientProvider, IDisposable {

	public Task<T> UseClientAsync<T>(Func<CosmosClient, Task<T>> consume)
		=> consume.Invoke(client);

	public void Dispose() {
		client.Dispose();
	}

}