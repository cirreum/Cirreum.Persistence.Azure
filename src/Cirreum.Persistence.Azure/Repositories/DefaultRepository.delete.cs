namespace Cirreum.Persistence;

using Cirreum.Exceptions;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

sealed partial class DefaultRepository<TEntity> {

	public async ValueTask DeleteAsync(
		TEntity value,
		CancellationToken cancellationToken = default) {
		await this.DeleteAsync(value, cancellationToken, true);
	}
	public async ValueTask<TEntity> DeleteAsync(
		TEntity value,
		CancellationToken cancellationToken = default,
		bool softDelete = true) {

		var pk = ValidatePartitionKey(value);

		var container = await _containerProvider.GetContainerAsync(_serviceKey).ConfigureAwait(false);

		(var optimizeBandwidth, var options) = this.RequestOptions;

		if (softDelete) {

			if (!typeof(IDeletableEntity).IsAssignableFrom(typeof(TEntity))) {
				throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} does not support soft delete");
			}

			if (value is IDeletableEntity deletable) {

				var user = await _userAccessor.GetUser();
				deletable.DeletedBy = user.Name;
				deletable.DeletedOn = this._datetimeService.UtcOffset;
				deletable.DeletedInTimeZone = this._datetimeService.LocalTimeZoneId;
				deletable.IsDeleted = true;

				if (value is IEtagEntity valueWithEtag) {
					if (string.IsNullOrWhiteSpace(valueWithEtag.Etag)) {
						throw new InvalidOperationException($"Cannot soft-delete entity with ID {value.Id}: Missing valid ETag");
					}
					options.IfMatchEtag = valueWithEtag.Etag;
				}

				try {

					var response = await container
						.ReplaceItemAsync(value, value.Id, pk, options, cancellationToken)
						.ConfigureAwait(false);

					_logger.LogItemSoftDeleted<TEntity>(value.Id);

					return optimizeBandwidth ? value : response.Resource;

				} catch (CosmosException e) when (e.StatusCode == HttpStatusCode.NotFound) {
					throw new NotFoundException(value.Id);
				} catch (CosmosException e) when (e.StatusCode == HttpStatusCode.PreconditionFailed) {
					throw new ConcurrencyException(value.Id);
				}
			} else {
				// This shouldn't happen due to type check above, but just in case
				throw new InvalidOperationException($"Entity with id {value.Id} does not support soft delete");
			}
		}

		// Hard delete path - requires explicit opt-in
		try {

			var response = await container
				.DeleteItemAsync<TEntity>(value.Id, pk, options, cancellationToken)
				.ConfigureAwait(false);

			this._logger.LogItemDeleted<TEntity>(value.Id);

			return optimizeBandwidth ? value : response.Resource;

		} catch (CosmosException e) when (e.StatusCode == HttpStatusCode.NotFound) {
			throw new NotFoundException(value.Id);
		} catch (CosmosException e) when (e.StatusCode == HttpStatusCode.PreconditionFailed) {
			throw new ConcurrencyException(value.Id);
		}

	}

	public async ValueTask DeleteAsync(
		string id,
		CancellationToken cancellationToken = default) {
		await this.DeleteAsync(id, cancellationToken, true);
	}
	public async ValueTask<TEntity> DeleteAsync(
		string id,
		CancellationToken cancellationToken,
		bool softDelete = true) {
		ArgumentException.ThrowIfNullOrWhiteSpace(id);

		if (softDelete) {

			if (!typeof(IDeletableEntity).IsAssignableFrom(typeof(TEntity))) {
				throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} does not support soft delete");
			}

			// First read the item
			var value =
				await this.GetAsync(id, false, cancellationToken)
				?? throw new NotFoundException(id);

			return await this.DeleteAsync(value, cancellationToken, softDelete);

		}

		// Hard delete path - requires explicit opt-in
		try {

			var options = this.RequestOptions.Options;
			var container = await this._containerProvider.GetContainerAsync(this._serviceKey).ConfigureAwait(false);

			var pk = ResolvePartitionKey(id);
			var response = await container
				.DeleteItemAsync<TEntity>(id, pk, options, cancellationToken)
				.ConfigureAwait(false);

			this._logger.LogItemDeleted<TEntity>(id);

			return response.Resource;

		} catch (CosmosException e) when (e.StatusCode == HttpStatusCode.NotFound) {
			throw new NotFoundException(id);
		} catch (CosmosException e) when (e.StatusCode == HttpStatusCode.PreconditionFailed) {
			throw new ConcurrencyException(id);
		}

	}

}