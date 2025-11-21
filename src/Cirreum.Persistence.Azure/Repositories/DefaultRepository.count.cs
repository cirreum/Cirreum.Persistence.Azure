namespace Cirreum.Persistence;

using Microsoft.Azure.Cosmos.Linq;

sealed partial class DefaultRepository<TEntity> {

	public async ValueTask<int> CountAsync(
		bool includeDeleted = false,
		CancellationToken cancellationToken = default) {

		var container = await this._containerProvider.GetContainerAsync(this._serviceKey).ConfigureAwait(false);

		IQueryable<TEntity> query = container.GetItemLinqQueryable<TEntity>(
			requestOptions: new QueryRequestOptions {
				MaxConcurrency = -1
			});

		if (!includeDeleted && typeof(IDeletableEntity).IsAssignableFrom(typeof(TEntity))) {
			query = query.Where(x => !((IDeletableEntity)x).IsDeleted);
		}

		this._logger.LogQueryConstructed<TEntity>($"Count: {query}");

		var response = await query.CountAsync(cancellationToken);

		this._logger.LogQueryExecuted(query, response.RequestCharge);

		return response.Resource;

	}

	public async ValueTask<int> CountAsync(
		Expression<Func<TEntity, bool>> predicate,
		bool includeDeleted = false,
		CancellationToken cancellationToken = default) {

		ArgumentNullException.ThrowIfNull(predicate);

		var container = await this._containerProvider.GetContainerAsync(this._serviceKey).ConfigureAwait(false);
		var finalPredicate = CombineWithDeleteFilter(predicate, includeDeleted);

		var query = container.GetItemLinqQueryable<TEntity>(
			requestOptions: new QueryRequestOptions {
				MaxConcurrency = -1
			})
			.Where(finalPredicate);

		this._logger.LogQueryConstructed<TEntity>($"Count: {query}");

		var response = await query.CountAsync(cancellationToken);

		this._logger.LogQueryExecuted(query, response.RequestCharge);

		return response.Resource;

	}

}