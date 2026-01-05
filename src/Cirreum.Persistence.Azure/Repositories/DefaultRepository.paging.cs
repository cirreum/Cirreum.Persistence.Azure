namespace Cirreum.Persistence;

using Microsoft.Azure.Cosmos.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

sealed partial class DefaultRepository<TEntity> {

	public async ValueTask<CursorResult<TEntity>> PageCursorAsync(
		Expression<Func<TEntity, bool>>? predicate,
		bool includeDeleted,
		int pageSize,
		string? cursor,
		CancellationToken cancellationToken = default) {

		var container = await this._containerProvider.GetContainerAsync(this._serviceKey).ConfigureAwait(false);

		var options = new QueryRequestOptions() {
			MaxConcurrency = -1,
			MaxItemCount = pageSize
		};

		IQueryable<TEntity> query = container
			.GetItemLinqQueryable<TEntity>(requestOptions: options, continuationToken: cursor);

		if (predicate is not null) {
			query = query.Where(predicate);
		}

		if (!includeDeleted && typeof(IDeletableEntity).IsAssignableFrom(typeof(TEntity))) {
			query = query.Where(x => !((IDeletableEntity)x).IsDeleted);
		}

		this._logger.LogQueryConstructed(query);

		(var items, var charge, var nextCursor) =
			await this._queryableProcessor.IterateAsync(query, pageSize, cancellationToken);

		this._logger.LogQueryExecuted(query, charge);

		return new CursorResult<TEntity>(
			items,
			nextCursor,
			nextCursor is not null);
	}

	public async ValueTask<PagedResult<TEntity>> PageAsync(
		Expression<Func<TEntity, bool>>? predicate,
		bool includeDeleted,
		int pageNumber,
		int pageSize,
		CancellationToken cancellationToken = default) {

		var container = await this._containerProvider.GetContainerAsync(this._serviceKey).ConfigureAwait(false);

		IQueryable<TEntity> query = container
			.GetItemLinqQueryable<TEntity>(
				requestOptions: new() {
					MaxConcurrency = -1,
					MaxItemCount = pageSize
				},
				linqSerializerOptions: new() {
					PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
				});

		if (predicate is not null) {
			query = query.Where(predicate);
		}

		if (!includeDeleted && typeof(IDeletableEntity).IsAssignableFrom(typeof(TEntity))) {
			query = query.Where(x => !((IDeletableEntity)x).IsDeleted);
		}

		// Get total count
		var countResponse = await query.CountAsync(cancellationToken).ConfigureAwait(false);
		var totalCount = countResponse.Resource;
		var countCharge = countResponse.RequestCharge;

		query = query
			.Skip(pageSize * (pageNumber - 1))
			.Take(pageSize);

		this._logger.LogQueryConstructed(query);

		(var items, var charge, var _) =
			await this._queryableProcessor.IterateAsync(query, pageSize, cancellationToken);

		this._logger.LogQueryExecuted(query, charge + countCharge);

		return new PagedResult<TEntity>(
			items,
			totalCount,
			pageSize,
			pageNumber);
	}

	public async ValueTask<SliceResult<TEntity>> SliceAsync(
		Expression<Func<TEntity, bool>>? predicate,
		bool includeDeleted,
		int count,
		CancellationToken cancellationToken = default) {

		var container = await this._containerProvider.GetContainerAsync(this._serviceKey).ConfigureAwait(false);

		// Fetch count + 1 to check if there are more items
		var fetchCount = count + 1;

		IQueryable<TEntity> query = container
			.GetItemLinqQueryable<TEntity>(
				requestOptions: new() {
					MaxConcurrency = -1,
					MaxItemCount = fetchCount
				},
				linqSerializerOptions: new() {
					PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
				});

		if (predicate is not null) {
			query = query.Where(predicate);
		}

		if (!includeDeleted && typeof(IDeletableEntity).IsAssignableFrom(typeof(TEntity))) {
			query = query.Where(x => !((IDeletableEntity)x).IsDeleted);
		}

		query = query.Take(fetchCount);

		this._logger.LogQueryConstructed(query);

		(var items, var charge, var _) =
			await this._queryableProcessor.IterateAsync(query, fetchCount, cancellationToken);

		this._logger.LogQueryExecuted(query, charge);

		var hasMore = items.Count > count;
		var resultItems = hasMore ? items.Take(count).ToList().AsReadOnly() : items;

		return new SliceResult<TEntity>(resultItems, hasMore);
	}

}
