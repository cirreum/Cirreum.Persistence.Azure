namespace Cirreum.Persistence;

using Microsoft.Azure.Cosmos.Linq;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

sealed partial class DefaultRepository<TEntity> {

	public async ValueTask<bool> ExistsAsync(
		string id,
		bool includeDeleted = false,
		CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(id);

		var pk = ResolvePartitionKey(id);
		var container = await this._containerProvider.GetContainerAsync(this._serviceKey).ConfigureAwait(false);

		try {

			var item = await container
				.ReadItemAsync<TEntity>(id, pk, cancellationToken: cancellationToken)
				.ConfigureAwait(false);

			if (!includeDeleted
				&& typeof(IDeletableEntity).IsAssignableFrom(typeof(TEntity))
				&& item is IDeletableEntity deletableEntity
				&& deletableEntity.IsDeleted) {
				return false;
			}

			return true;

		} catch (CosmosException e) when (e.StatusCode == HttpStatusCode.NotFound) {
			return false;
		}

	}

	public async ValueTask<bool> ExistsAsync(
		Expression<Func<TEntity, bool>> predicate,
		bool includeDeleted,
		CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(predicate);

		var container = await _containerProvider.GetContainerAsync(_serviceKey).ConfigureAwait(false);
		var finalPredicate = DefaultRepository<TEntity>.CombineWithDeleteFilter(predicate, includeDeleted);

		var query = container
			.GetItemLinqQueryable<TEntity>(
				requestOptions: new QueryRequestOptions {
					MaxConcurrency = -1,
					MaxItemCount = 1
				})
			.Where(finalPredicate)
			.Select(e => 1)  // Only select a scalar value
			.Take(1);        // Limit to one result


		using var iterator = query.ToFeedIterator();
		var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);

		return response.Count != 0;

	}

}