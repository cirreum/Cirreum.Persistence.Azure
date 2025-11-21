namespace Cirreum.Persistence;

using Microsoft.Azure.Cosmos.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

sealed partial class DefaultRepository<TEntity> {

	public async ValueTask<IContinuationPage<TEntity>> PageContinuationAsync(
		Expression<Func<TEntity, bool>>? predicate = null,
		bool includeDeleted = false,
		int pageSize = 25,
		string? continuationToken = null,
		CancellationToken cancellationToken = default) {

		var container = await this._containerProvider.GetContainerAsync(this._serviceKey).ConfigureAwait(false);

		var options = new QueryRequestOptions() {
			MaxConcurrency = -1,
			MaxItemCount = pageSize
		};

		IQueryable<TEntity> query = container
			.GetItemLinqQueryable<TEntity>(requestOptions: options, continuationToken: continuationToken);

		if (predicate is not null) {
			query = query.Where(predicate);
		}

		if (!includeDeleted && typeof(IDeletableEntity).IsAssignableFrom(typeof(TEntity))) {
			query = query.Where(x => !((IDeletableEntity)x).IsDeleted);
		}

		this._logger.LogQueryConstructed(query);

		(var items, var charge, var ContinuationToken) =
			await this._queryableProcessor.IterateAsync(query, pageSize, cancellationToken);

		this._logger.LogQueryExecuted(query, charge);

		return new ContinuationPage<TEntity>(
			items.Count,
			pageSize,
			items,
			charge,
			ContinuationToken);
	}

	public async ValueTask<IOffSetPage<TEntity>> PageOffsetAsync(
		Expression<Func<TEntity, bool>>? predicate = null,
		bool includeDeleted = false,
		int pageNumber = 1,
		int pageSize = 25,
		bool includeTotalCount = false,
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

		int? count = default;
		var countCharge = 0.0;
		if (includeTotalCount) {
			var countResponse = await query.CountAsync(cancellationToken).ConfigureAwait(false);
			count = countResponse.Resource;
			countCharge = countResponse.RequestCharge;
		}

		query = query
			.Skip(pageSize * (pageNumber - 1))
			.Take(pageSize);

		this._logger.LogQueryConstructed(query);

		(var items, var charge, var _) =
			await this._queryableProcessor.IterateAsync(query, pageSize, cancellationToken);

		this._logger.LogQueryExecuted(query, charge);

		return new OffsetPage<TEntity>(
			count,
			pageNumber,
			pageSize,
			items,
			charge + countCharge);

	}

}