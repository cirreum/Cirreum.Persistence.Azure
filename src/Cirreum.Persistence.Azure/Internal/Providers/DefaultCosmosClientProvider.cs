namespace Cirreum.Persistence.Internal.Providers;

using Microsoft.Azure.Cosmos;

/// <summary>
/// Default implementation of ICosmosClientProvider that wraps a CosmosClient instance.
/// </summary>
sealed class DefaultCosmosClientProvider(CosmosClient client) : ICosmosClientProvider, IDisposable {

	public T UseClient<T>(Func<CosmosClient, T> consume) {
		ArgumentNullException.ThrowIfNull(consume);
		return consume(client);
	}

	public Task<T> UseClientAsync<T>(Func<CosmosClient, Task<T>> consume) {
		ArgumentNullException.ThrowIfNull(consume);
		return consume(client);
	}

	public void Dispose() {
		client.Dispose();
	}

}