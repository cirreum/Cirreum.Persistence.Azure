namespace Cirreum.Persistence;

using Cirreum.Exceptions;
using System.Collections.Generic;

sealed partial class InMemoryRepository<TEntity> {

	public async ValueTask UpdateAsBatchAsync(
	  IEnumerable<TEntity> items,
	  CancellationToken cancellationToken = default) {
		foreach (var item in items) {
			await this.UpdateAsync(item, cancellationToken);
		}
	}

	public async ValueTask CreateAsBatchAsync(
		IEnumerable<TEntity> items,
		CancellationToken cancellationToken = default) {
		foreach (var item in items) {
			await this.CreateAsync(item, cancellationToken);
		}
	}

	public async ValueTask DeleteAsBatchAsync(
		IEnumerable<TEntity> items,
		CancellationToken cancellationToken = default) {
		var list = items.ToList();
		_ = GetPartitionKeyValue(list); // Validates all items have same partition key
		foreach (var item in items) {
			await this.DeleteAsync(item, cancellationToken);
		}
	}

	public async ValueTask DeleteAsBatchAsync(
		IEnumerable<TEntity> items,
		CancellationToken cancellationToken = default,
		bool softDelete = true) {
		var list = items.ToList();
		_ = GetPartitionKeyValue(list); // Validates all items have same partition key
		foreach (var item in items) {
			await this.DeleteAsync(item, cancellationToken, softDelete);
		}
	}

	public ValueTask<IEnumerable<(string Id, bool Restored)>> RestoreAsBatchAsync(
		IEnumerable<TEntity> items,
		CancellationToken cancellationToken = default) {
		var list = items.ToList();
		_ = GetPartitionKeyValue(list); // Validates all items have same partition key

		var results = new List<(string Id, bool Restored)>();

		foreach (var item in items) {
			if (cancellationToken.IsCancellationRequested) {
				break;
			}

			if (item is not IRestorableEntity restorable) {
				throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} does not support restore operations");
			}

			if (!restorable.IsDeleted) {
				results.Add((item.Id, false));
				continue;
			}

			restorable.IsDeleted = false;
			restorable.DeletedBy = string.Empty;
			restorable.DeletedOn = null;
			restorable.DeletedInTimeZone = null;
			restorable.RestoreCount++;

			// Update in storage
			InMemoryStorage.GetDictionary<TEntity>()[item.Id] = SerializeItem(item);
			results.Add((item.Id, true));
		}

		return new ValueTask<IEnumerable<(string Id, bool Restored)>>(results);

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