namespace Cirreum.Persistence;

using Cirreum.Exceptions;
using System;

sealed partial class DefaultRepository<TEntity> {

	public async ValueTask UpdateAsBatchAsync(
		IEnumerable<TEntity> items,
		CancellationToken cancellationToken = default) {

		var list = items.ToList();

		// Check for soft-deleted items first
		foreach (var item in list) {
			if (item is IDeletableEntity deletable && deletable.IsDeleted) {
				throw new InvalidOperationException(
					$"Entity {item.Id} is soft-deleted and cannot be modified.");
			}
		}

		var partitionKey = GetPartitionKeyValue(list);

		var container = await this._containerProvider.GetContainerAsync(this._serviceKey).ConfigureAwait(false);

		var batch = container.CreateTransactionalBatch(new PartitionKey(partitionKey));

		var user = await this._userAccessor.GetUser();
		var userName = user.Name;

		foreach (var item in list) {

			if (item is IAuditableEntity auditable) {
				auditable.ModifiedBy = userName;
				auditable.ModifiedInTimeZone = this._datetimeService.LocalTimeZoneId;
			}

			if (item is IEtagEntity etagEntity) {
				if (string.IsNullOrWhiteSpace(etagEntity.Etag)) {
					throw new InvalidOperationException($"Cannot delete entity with ID {etagEntity.Id}: Missing valid ETag");
				}
				var options = new TransactionalBatchItemRequestOptions {
					IfMatchEtag = etagEntity.Etag
				};
				batch.UpsertItem(item, options);
			} else {
				batch.UpsertItem(item);
			}


		}

		using var response = await batch.ExecuteAsync(cancellationToken);

		if (!response.IsSuccessStatusCode) {
			throw new BatchOperationException(response.StatusCode, typeof(TEntity).Name);
		}

	}

	public async ValueTask CreateAsBatchAsync(
		IEnumerable<TEntity> items,
		CancellationToken cancellationToken = default) {
		var list = items.ToList();

		var partitionKey = GetPartitionKeyValue(list);

		var container = await this._containerProvider.GetContainerAsync(this._serviceKey).ConfigureAwait(false);

		var batch = container.CreateTransactionalBatch(new PartitionKey(partitionKey));

		var user = await this._userAccessor.GetUser();
		var userName = user.Name;
		foreach (var item in list) {
			this.PrepareCreateItem(item, userName);
			batch.CreateItem(item);
		}

		using var response = await batch.ExecuteAsync(cancellationToken);

		if (!response.IsSuccessStatusCode) {
			throw new BatchOperationException(response.StatusCode, typeof(TEntity).Name);
		}

	}

	public ValueTask DeleteAsBatchAsync(
		IEnumerable<TEntity> items,
		CancellationToken cancellationToken = default)
		=> this.DeleteAsBatchAsync(items, cancellationToken, true);

	public async ValueTask DeleteAsBatchAsync(
		IEnumerable<TEntity> items,
		CancellationToken cancellationToken = default,
		bool softDelete = true) {

		var list = items.ToList();
		var partitionKey = GetPartitionKeyValue(list);
		var container = await _containerProvider.GetContainerAsync(_serviceKey).ConfigureAwait(false);
		var batch = container.CreateTransactionalBatch(new PartitionKey(partitionKey));

		if (softDelete) {

			if (!typeof(IDeletableEntity).IsAssignableFrom(typeof(TEntity))) {
				throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} does not support soft delete");
			}

			var user = await _userAccessor.GetUser();
			var userName = user.Name;

			foreach (var item in list) {
				if (item is IDeletableEntity deletable) {
					deletable.DeletedBy = userName;
					deletable.DeletedOn = this._datetimeService.UtcOffset;
					deletable.DeletedInTimeZone = this._datetimeService.LocalTimeZoneId;
					deletable.IsDeleted = true;

					if (item is IEtagEntity etagEntity) {
						if (string.IsNullOrWhiteSpace(etagEntity.Etag)) {
							throw new InvalidOperationException($"Cannot soft-delete batch entity with ID {etagEntity.Id}: Missing valid ETag");
						}
						var options = new TransactionalBatchItemRequestOptions {
							IfMatchEtag = etagEntity.Etag
						};
						batch.ReplaceItem(item.Id, item, options);
					} else {
						batch.ReplaceItem(item.Id, item);
					}

					_logger.LogItemSoftDeleted<TEntity>(item.Id);
				} else {
					// This shouldn't happen due to type check above, but just in case
					throw new InvalidOperationException($"Entity with id {item.Id} does not support soft delete");
				}
			}

		} else {
			// Hard delete path - requires explicit opt-in
			foreach (var item in list) {

				if (item is IEtagEntity etagEntity) {
					if (string.IsNullOrWhiteSpace(etagEntity.Etag)) {
						throw new InvalidOperationException($"Cannot hard-delete batch entity with ID {etagEntity.Id}: Missing valid ETag");
					}
					var options = new TransactionalBatchItemRequestOptions {
						IfMatchEtag = etagEntity.Etag
					};
					batch.DeleteItem(item.Id, options);
				} else {
					batch.DeleteItem(item.Id);
				}

				_logger.LogItemDeleted<TEntity>(item.Id);
			}
		}

		using var response = await batch.ExecuteAsync(cancellationToken);

		if (!response.IsSuccessStatusCode) {
			// Could enhance BatchOperationException to include more details from response
			throw new BatchOperationException(response.StatusCode, typeof(TEntity).Name);
		}

	}

	public async ValueTask<IEnumerable<(string Id, bool Restored)>> RestoreAsBatchAsync(
		IEnumerable<TEntity> items,
		CancellationToken cancellationToken = default) {
		var list = items.ToList();
		var partitionKey = GetPartitionKeyValue(list);
		var container = await _containerProvider.GetContainerAsync(_serviceKey).ConfigureAwait(false);
		var batch = container.CreateTransactionalBatch(new PartitionKey(partitionKey));

		var results = new List<(string Id, bool Restored)>();

		foreach (var item in list) {
			if (item is not IRestorableEntity restorable) {
				throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} does not support restore operations");
			}

			var wasDeleted = restorable.IsDeleted;
			if (wasDeleted) {
				restorable.IsDeleted = false;
				restorable.DeletedBy = string.Empty;
				restorable.DeletedOn = null;
				restorable.DeletedInTimeZone = null;
				restorable.RestoreCount++;

				if (item is IEtagEntity etagEntity) {
					if (string.IsNullOrWhiteSpace(etagEntity.Etag)) {
						throw new InvalidOperationException($"Cannot restore batch entity with ID {etagEntity.Id}: Missing valid ETag");
					}
					var options = new TransactionalBatchItemRequestOptions {
						IfMatchEtag = etagEntity.Etag
					};
					batch.ReplaceItem(item.Id, item, options);
				} else {
					batch.ReplaceItem(item.Id, item);
				}

			}

			results.Add((item.Id, wasDeleted));
		}

		using var response = await batch.ExecuteAsync(cancellationToken);

		if (!response.IsSuccessStatusCode) {
			throw new BatchOperationException(response.StatusCode, typeof(TEntity).Name);
		}

		return results;
	}

	private static string GetPartitionKeyValue(List<TEntity> items) {
		if (items.Count == 0) {
			throw new ArgumentException(
				"Unable to perform batch operation with no items",
				nameof(items));
		}

		var partitionKey = items[0].PartitionKey;

		// Validate all items have same partition key
		if (items.Any(item => item.PartitionKey != partitionKey)) {
			throw new BadRequestException(
				"All items in a batch operation must share the same partition key");
		}

		return partitionKey;
	}

}