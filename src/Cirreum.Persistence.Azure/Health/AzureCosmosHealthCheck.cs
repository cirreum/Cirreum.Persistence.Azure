namespace Cirreum.Persistence.Health;

using Cirreum.Persistence.Internal.Providers;
using Cirreum.ServiceProvider.Health;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Threading.Tasks;

sealed class AzureCosmosHealthCheck(
	ICosmosClientProvider clientProvider,
	bool isProduction,
	IMemoryCache memoryCache,
	AzureCosmosHealthCheckOptions options
) : IServiceProviderHealthCheck<AzureCosmosHealthCheckOptions>
  , IDisposable {

	private readonly ICosmosClientProvider _clientProvider = clientProvider;
	private readonly AzureCosmosHealthCheckOptions _options = options;
	private readonly string _cacheKey = $"_azure_cosmosdb_health_{clientProvider.GetType().Name}";
	private readonly TimeSpan _cacheDuration = options.CachedResultTimeout ?? TimeSpan.FromSeconds(60);
	private readonly TimeSpan _failureCacheDuration = TimeSpan.FromSeconds(Math.Max(35, (options.CachedResultTimeout ?? TimeSpan.FromSeconds(60)).TotalSeconds / 2));
	private readonly bool _cacheDisabled = (options.CachedResultTimeout is null || options.CachedResultTimeout.Value.TotalSeconds == 0);
	private readonly SemaphoreSlim _semaphore = new(1, 1);

	public async Task<HealthCheckResult> CheckHealthAsync(
		HealthCheckContext context,
		CancellationToken cancellationToken = default) {

		if (this._cacheDisabled) {
			// No caching...
			return await _clientProvider.UseClientAsync(c =>
				CheckCosmosDbHealthAsync(c, context, this._options, isProduction, cancellationToken))
				.ConfigureAwait(false);
		}

		// Try get from cache first
		if (memoryCache.TryGetValue(this._cacheKey, out HealthCheckResult cachedResult)) {
			return cachedResult;
		}

		// If not in cache, ensure only one thread updates it
		try {

			await this._semaphore.WaitAsync(cancellationToken);

			// Double-check after acquiring semaphore
			if (memoryCache.TryGetValue(this._cacheKey, out cachedResult)) {
				return cachedResult;
			}

			// Perform actual health check
			var result = await this._clientProvider.UseClientAsync(c =>
				CheckCosmosDbHealthAsync(c, context, this._options, isProduction, cancellationToken))
				.ConfigureAwait(false);

			// Cache with appropriate duration based on health status
			var jitter = TimeSpan.FromSeconds(Random.Shared.Next(0, 5));
			var duration = result.Status == HealthStatus.Healthy
				? this._cacheDuration
				: this._failureCacheDuration;

			return memoryCache.Set(this._cacheKey, result, duration + jitter);

		} finally {
			this._semaphore.Release();
		}

	}

	static async Task<HealthCheckResult> CheckCosmosDbHealthAsync(
		CosmosClient client,
		HealthCheckContext context,
		AzureCosmosHealthCheckOptions options,
		bool isProduction,
		CancellationToken cancellationToken = default) {

		try {
			var props = await client.ReadAccountAsync().ConfigureAwait(false);
		} catch (AggregateException aex) {
			return new HealthCheckResult(context.Registration.FailureStatus, exception: aex.InnerException ?? aex);
		} catch (Exception ex) {
			return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
		}

		if (string.IsNullOrWhiteSpace(options.DatabaseId)) {
			if (isProduction) {
				return HealthCheckResult.Healthy($"Connected to cosmosdb service");
			}
			return HealthCheckResult.Healthy($"Connected to cosmosdb service: {client.Endpoint}");
		}

		try {

			var database = client.GetDatabase(options.DatabaseId);
			var props = await database.ReadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

			if (options.ContainerIds is null || options.ContainerIds.Count == 0) {
				if (isProduction) {
					return HealthCheckResult.Healthy($"Connected to cosmosdb service and database");
				}
				return HealthCheckResult.Healthy($"Connected to cosmosdb service: {client.Endpoint} with database {options.DatabaseId}");
			}

			var testedContainer = new List<string>();
			foreach (var container in options.ContainerIds) {
				if (string.IsNullOrWhiteSpace(container) is false) {
					try {
						await database
							.GetContainer(container)
							.ReadContainerAsync(cancellationToken: cancellationToken)
							.ConfigureAwait(false);
						testedContainer.Add(container);
					} catch (AggregateException aex) {
						return new HealthCheckResult(HealthStatus.Degraded, exception: aex.InnerException ?? aex);
					} catch (Exception ex) {
						return new HealthCheckResult(HealthStatus.Degraded, exception: ex);
					}
				}
			}

			if (isProduction) {
				return HealthCheckResult.Healthy($"Connected to cosmosdb service, database and 1 or more containers");
			}
			return HealthCheckResult.Healthy(
				$"Connected to cosmosdb service: {client.Endpoint} with database {options.DatabaseId} and the following container(s): {string.Join(',', testedContainer)}");

		} catch (AggregateException aex) {
			return new HealthCheckResult(
				context.Registration.FailureStatus,
				exception: aex.InnerException ?? aex);
		} catch (Exception ex) {
			return new HealthCheckResult(
				context.Registration.FailureStatus,
				exception: ex);
		}

	}

	public void Dispose() {
		this._semaphore?.Dispose();
	}

}