namespace Cirreum.Persistence;

using Cirreum.Exceptions;
using Humanizer;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

sealed partial class DefaultRepository<TEntity> {

	public ValueTask<TEntity> UpdateAsync(
		TEntity value,
		CancellationToken cancellationToken = default) =>
		this.UpdateAsync(value, false, cancellationToken);

	public async ValueTask<TEntity> UpdateAsync(
		TEntity value,
		bool ignoreEtag = false,
		CancellationToken cancellationToken = default) {

		// First check if entity is already soft deleted
		if (value is IDeletableEntity deletable && deletable.IsDeleted) {
			throw new InvalidOperationException(
				$"Entity {value.Id} is soft-deleted. Use RestoreAsync to re-enable the entity.");
		}

		if (value is IAuditableEntity auditable) {
			var user = await this._userAccessor.GetUser();
			// We use the CosmosDb _ts value from
			// See the ModifyOnRaw property
			//auditable.ModifiedOn = this._datetimeService.UtcOffset;
			auditable.ModifiedBy = user.Name;
			auditable.ModifiedInTimeZone = this._datetimeService.LocalTimeZoneId;
		}

		var pk = ValidatePartitionKey(value);

		var container = await this._containerProvider.GetContainerAsync(this._serviceKey).ConfigureAwait(false);

		(var optimizeBandwidth, var options) = this.RequestOptions;

		if (value is IEtagEntity valueWithEtag && !ignoreEtag) {
			if (string.IsNullOrWhiteSpace(valueWithEtag.Etag)) {
				throw new InvalidOperationException($"Cannot update entity with ID {value.Id}: Missing valid ETag");
			}
			options.IfMatchEtag = valueWithEtag.Etag;
		}

		try {

			var response =
				await container
					.UpsertItemAsync(value, pk, options, cancellationToken)
					.ConfigureAwait(false);

			this._logger.LogItemUpdated(value);

			return optimizeBandwidth ? value : response.Resource;

		} catch (CosmosException e) when (e.StatusCode == HttpStatusCode.NotFound) {
			throw new NotFoundException(value.Id);
		} catch (CosmosException e) when (e.StatusCode == HttpStatusCode.PreconditionFailed) {
			throw new ConcurrencyException(value.Id);
		}

	}

	public async ValueTask<IEnumerable<TEntity>> UpdateAsync(
		IEnumerable<TEntity> values,
		bool ignoreEtag = false,
		CancellationToken cancellationToken = default) {

		var updateTasks = values
				.Select(value => this.UpdateAsync(value, ignoreEtag, cancellationToken).AsTask())
				.ToList();

		await Task.WhenAll(updateTasks).ConfigureAwait(false);

		return updateTasks.Select(x => x.Result);

	}

	public async ValueTask UpdatePartialAsync(
		string id,
		Action<IPatchOperationBuilder<TEntity>> builder,
		string? etag = default,
		CancellationToken cancellationToken = default) {

		var patchOperationBuilder = new PatchOperationBuilder<TEntity>();

		builder(patchOperationBuilder);

		var container = await this._containerProvider.GetContainerAsync(this._serviceKey).ConfigureAwait(false);

		var pk = ResolvePartitionKey(id);

		var patchItemRequestOptions = new PatchItemRequestOptions();
		if (etag != default && string.IsNullOrWhiteSpace(etag) is false) {
			patchItemRequestOptions.IfMatchEtag = etag;
		}

		try {

			if (typeof(IAuditableEntity).IsAssignableFrom(typeof(TEntity))) {
				var user = await this._userAccessor.GetUser();
				patchOperationBuilder.SetByPath(nameof(IAuditableEntity.ModifiedBy).Camelize(), user.Name);
				patchOperationBuilder.SetByPath(nameof(IAuditableEntity.ModifiedInTimeZone).Camelize(), this._datetimeService.LocalTimeZoneId);
			}

			await container.PatchItemAsync<TEntity>(
				id,
				pk,
				patchOperationBuilder.PatchOperations,
				patchItemRequestOptions,
				cancellationToken);

		} catch (CosmosException e) when (e.StatusCode == HttpStatusCode.NotFound) {
			throw new NotFoundException(id);
		} catch (CosmosException e) when (e.StatusCode == HttpStatusCode.PreconditionFailed) {
			throw new ConcurrencyException(id);
		}

	}


	public async ValueTask<(bool Restored, TEntity Entity)> RestoreAsync(
		string id,
		CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(id);

		if (!typeof(IRestorableEntity).IsAssignableFrom(typeof(TEntity))) {
			throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} does not support restore operations");
		}

		var value =
			await this.GetAsync(id, true, cancellationToken)
			?? throw new NotFoundException(id);

		if (value is IRestorableEntity restorable) {

			if (!restorable.IsDeleted) {
				return (false, value);
			}

			// FUTURE: Consider saving deletion history

			// Clear deletion info
			restorable.IsDeleted = false;
			restorable.DeletedBy = string.Empty;
			restorable.DeletedOn = null;
			restorable.DeletedInTimeZone = null;
			restorable.RestoreCount++;

			var updatedEntity = await this.UpdateAsync(value, cancellationToken);

			return (true, updatedEntity);

		}


		throw new InvalidOperationException($"Entity with id {id} does not support restore operations");

	}

}