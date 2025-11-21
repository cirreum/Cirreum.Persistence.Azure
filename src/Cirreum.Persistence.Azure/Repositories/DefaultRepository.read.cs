namespace Cirreum.Persistence;

using Cirreum.Exceptions;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

sealed partial class DefaultRepository<TEntity> {

	public async ValueTask<TEntity> GetAsync(
		string id,
		bool includeDeleted = false,
		CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(id);

		var partitionKey = ResolvePartitionKey(id);

		var container = await this._containerProvider.GetContainerAsync(this._serviceKey).ConfigureAwait(false);

		this._logger.LogPointReadStarted<TEntity>(id, partitionKey.ToString());

		try {

			var response =
				await container
					.ReadItemAsync<TEntity>(id, partitionKey, cancellationToken: cancellationToken)
					.ConfigureAwait(false);

			var item = response.Resource;

			if (!includeDeleted &&
				typeof(IDeletableEntity).IsAssignableFrom(typeof(TEntity)) &&
				item is IDeletableEntity deletable &&
				deletable.IsDeleted) {
				var notFoundEx = new NotFoundException(id);
				this._logger.LogPointReadException<TEntity>(response.RequestCharge, notFoundEx);
				throw notFoundEx;
			}

			this._logger.LogPointReadExecuted<TEntity>(response.RequestCharge);
			this._logger.LogItemRead(item);

			return item;

		} catch (CosmosException e) when (e.StatusCode == HttpStatusCode.NotFound) {
			this._logger.LogPointReadException<TEntity>(e.RequestCharge, e);
			throw new NotFoundException(id);
		}

	}


	public async ValueTask<IReadOnlyList<TEntity>> GetManyAsync(
		IEnumerable<string> ids,
		bool includeDeleted = false,
		CancellationToken cancellationToken = default) {

		var container = await this._containerProvider.GetContainerAsync(this._serviceKey).ConfigureAwait(false);

		try {

			var requestItems = new List<(string, PartitionKey)>();
			foreach (var id in ids) {
				requestItems.Add((id, ResolvePartitionKey(id)));
			}

			var response = await container
				.ReadManyItemsAsync<TEntity>(requestItems, null, cancellationToken)
				.ConfigureAwait(false);

			IReadOnlyList<TEntity> items = [.. response.Resource];

			if (!includeDeleted && typeof(IDeletableEntity).IsAssignableFrom(typeof(TEntity))) {
				items = [.. items.Where(x => !((IDeletableEntity)x).IsDeleted)];
			}

			return items;

		} catch (CosmosException e) {
			if (e.StatusCode == HttpStatusCode.NotFound) {
				throw new NotFoundException([.. ids]);
			}
			throw;
		}

	}


	public async ValueTask<IReadOnlyList<TEntity>> GetAllAsync(
		bool includeDeleted = false,
		CancellationToken cancellationToken = default) {

		var container = await this._containerProvider.GetContainerAsync(this._serviceKey).ConfigureAwait(false);
		IReadOnlyList<TEntity> items;
		double charge;

		if (!includeDeleted && typeof(IDeletableEntity).IsAssignableFrom(typeof(TEntity))) {
			const string query = "SELECT * FROM c WHERE (IS_DEFINED(c.isDeleted) = false OR c.isDeleted = false)";
			this._logger.LogQueryConstructed<TEntity>($"GetAll (Filtered): {query}");
			return await this.QueryAsync(query, cancellationToken);
		}

		this._logger.LogQueryConstructed<TEntity>("GetAll");

		(items, charge) =
			await this._queryableProcessor.IterateGetAllAsync<TEntity>(container, cancellationToken: cancellationToken);

		this._logger.LogQueryExecuted<TEntity>("GetAll", charge);
		return items;

	}

}