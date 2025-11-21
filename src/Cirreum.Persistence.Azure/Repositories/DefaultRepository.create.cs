namespace Cirreum.Persistence;

using Cirreum.Exceptions;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

sealed partial class DefaultRepository<TEntity> {

	public async ValueTask<TEntity> CreateAsync(
		TEntity value,
		CancellationToken cancellationToken = default) {

		var container = await this._containerProvider.GetContainerAsync(this._serviceKey).ConfigureAwait(false);
		(var optimizeBandwidth, var options) = this.RequestOptions;
		return await this.CreateAsync(container, value, options, optimizeBandwidth, cancellationToken)
						.ConfigureAwait(false);

	}

	public async ValueTask<IEnumerable<TEntity>> CreateAsync(
		IEnumerable<TEntity> values,
		CancellationToken cancellationToken = default) {

		var container = await this._containerProvider.GetContainerAsync(this._serviceKey).ConfigureAwait(false);
		(var optimizeBandwidth, var options) = this.RequestOptions;

		var creationTasks = values
				.Select(value => this.CreateAsync(container, value, options, optimizeBandwidth, cancellationToken).AsTask())
				.ToList();

		await Task.WhenAll(creationTasks).ConfigureAwait(false);

		return creationTasks.Select(x => x.Result);

	}

	private void PrepareCreateItem(TEntity entity, string userName) {

		entity.Id = GenerateItemId(entity);

		if (entity is IAuditableEntity auditable) {
			auditable.CreatedInTimeZone = this._datetimeService.LocalTimeZoneId;
			auditable.CreatedOn = this._datetimeService.UtcOffset;
			auditable.CreatedBy = userName;
		}

		// Initialize soft-delete properties
		if (entity is IDeletableEntity deletable) {
			deletable.IsDeleted = false;
			deletable.DeletedBy = string.Empty;
			deletable.DeletedOn = null;
			deletable.DeletedInTimeZone = null;

			// If it's also restorable, initialize restore count
			if (entity is IRestorableEntity restorable) {
				restorable.RestoreCount = 0;
			}
		}

	}

	private async ValueTask<TEntity> CreateAsync(
		Container container,
		TEntity value,
		ItemRequestOptions options,
		bool optimizeBandwidth,
		CancellationToken cancellationToken = default) {

		var user = await this._userAccessor.GetUser();
		var userName = user.Name;
		this.PrepareCreateItem(value, userName);

		try {

			var response =
				await container
					.CreateItemAsync(value, new PartitionKey(value.PartitionKey), options, cancellationToken)
					.ConfigureAwait(false);

			this._logger.LogItemCreated(value);

			return optimizeBandwidth ? value : response.Resource;

		} catch (CosmosException e) when (e.StatusCode == HttpStatusCode.Conflict) {
			throw new AlreadyExistsException($"Item {value.Id} already exists.", e);
		}

	}

}