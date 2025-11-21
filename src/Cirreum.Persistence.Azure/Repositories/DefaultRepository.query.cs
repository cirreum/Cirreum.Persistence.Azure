namespace Cirreum.Persistence;

using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

sealed partial class DefaultRepository<TEntity> {

	public async ValueTask<TEntity?> FirstOrNullAsync(
		Expression<Func<TEntity, bool>> predicate,
		bool includeDeleted,
		CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(predicate);

		var container = await this._containerProvider.GetContainerAsync(this._serviceKey).ConfigureAwait(false);
		var finalPredicate = DefaultRepository<TEntity>.CombineWithDeleteFilter(predicate, includeDeleted);

		var query = container
			.GetItemLinqQueryable<TEntity>(
				requestOptions: new QueryRequestOptions {
					MaxConcurrency = -1,
					MaxItemCount = 1
				},
				linqSerializerOptions: new CosmosLinqSerializerOptions {
					PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
				})
		   .Where(finalPredicate)
		   .Take(1);

		_logger.LogQueryConstructed<TEntity>($"FirstOrNull: {query}");

		using var iterator = query.ToFeedIterator();
		var response = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);

		_logger.LogQueryExecuted(query, response.RequestCharge);

		return response.Resource.FirstOrDefault();

	}

	public async ValueTask<IReadOnlyList<TEntity>> QueryAsync(
		Expression<Func<TEntity, bool>> predicate,
		bool includeDeleted,
		CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(predicate);

		var container = await this._containerProvider.GetContainerAsync(this._serviceKey).ConfigureAwait(false);

		var finalPredicate = DefaultRepository<TEntity>.CombineWithDeleteFilter(predicate, includeDeleted);

		var query =
			container
				.GetItemLinqQueryable<TEntity>(
					requestOptions: new QueryRequestOptions {
						MaxConcurrency = -1
					},
					linqSerializerOptions: new CosmosLinqSerializerOptions {
						PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
					})
				.Where(finalPredicate);

		this._logger.LogQueryConstructed(query);

		(var items, var charge) =
			await this._queryableProcessor.IterateAsync(query, cancellationToken);

		this._logger.LogQueryExecuted(query, charge);

		return items;

	}

	public async ValueTask<IReadOnlyList<TEntity>> QueryAsync(
		string query,
		CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(query);
		var queryDefinition = new QueryDefinition(query);

		var container = await this._containerProvider.GetContainerAsync(this._serviceKey).ConfigureAwait(false);

		this._logger.LogQueryConstructed<TEntity>(query);

		(var items, var charge) =
			await this._queryableProcessor.IterateAsync<TEntity>(container, queryDefinition, cancellationToken);

		this._logger.LogQueryExecuted<TEntity>(query, charge);

		return items;

	}

	public async ValueTask<IReadOnlyList<TEntity>> QueryAsync(
		string parameterizedQuery,
		IEnumerable<KeyValuePair<string, string>> parameters,
		CancellationToken cancellationToken = default) {

		ArgumentException.ThrowIfNullOrWhiteSpace(parameterizedQuery);
		var queryDefinition = new QueryDefinition(parameterizedQuery);
		foreach (var p in parameters) {
			queryDefinition = queryDefinition
				.WithParameter(p.Key, p.Value);
		}

		var container = await this._containerProvider.GetContainerAsync(this._serviceKey).ConfigureAwait(false);

		this._logger.LogQueryConstructed<TEntity>(queryDefinition.QueryText);

		(var items, var charge) =
			await this._queryableProcessor.IterateAsync<TEntity>(container, queryDefinition, cancellationToken);

		this._logger.LogQueryExecuted<TEntity>(parameterizedQuery, charge);

		return items;

	}

	public async IAsyncEnumerable<TEntity> SequenceQueryAsync(
		Expression<Func<TEntity, bool>> predicate,
		bool includeDeleted,
		int? maxResults = null,
		[EnumeratorCancellation] CancellationToken cancellationToken = default) {
		ArgumentNullException.ThrowIfNull(predicate);

		var container = await this._containerProvider.GetContainerAsync(this._serviceKey).ConfigureAwait(false);
		var finalPredicate = DefaultRepository<TEntity>.CombineWithDeleteFilter(predicate, includeDeleted);

		var itemsReturned = 0;
		var query = container
			.GetItemLinqQueryable<TEntity>(
				requestOptions: new QueryRequestOptions() {
					MaxItemCount = maxResults ?? -1
				},
				linqSerializerOptions: new CosmosLinqSerializerOptions {
					PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
				}
			)
			.Where(finalPredicate);

		if (this._logger.IsEnabled(LogLevel.Debug)) {
			this._logger.LogQueryConstructed(query);
		}

		using (var iterator = query.ToFeedIterator()) {
			while (iterator.HasMoreResults) {

				var feed = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);

				if (this._logger.IsEnabled(LogLevel.Debug)) {
					this._logger.LogQueryExecuted(query, feed.RequestCharge);
					if (feed.Diagnostics != null) {
						this._logger.LogQueryDiagnostics<TEntity>(feed.Diagnostics.ToJson());
					}
				}

				foreach (var item in feed) {
					yield return item;
					itemsReturned++;
					if (maxResults.HasValue && itemsReturned >= maxResults.Value) {
						yield break;
					}
				}

			}
		}

	}

}