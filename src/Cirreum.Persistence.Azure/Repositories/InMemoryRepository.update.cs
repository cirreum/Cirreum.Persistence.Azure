namespace Cirreum.Persistence;

sealed partial class InMemoryRepository<TEntity> {

	public ValueTask<TEntity> UpdateAsync(
		TEntity value,
		CancellationToken cancellationToken = default) =>
		this.UpdateAsync(value, true, cancellationToken);

	public ValueTask<TEntity> UpdateAsync(
		TEntity value,
		bool ignoreEtag = false,
		CancellationToken cancellationToken = default) =>
		this.UpdateAsync(value, ignoreEtag);

	public async ValueTask<IEnumerable<TEntity>> UpdateAsync(
		IEnumerable<TEntity> values,
		bool ignoreEtag = false,
		CancellationToken cancellationToken = default) {

		List<TEntity> results = [];

		foreach (var value in values) {
			results.Add(await this.UpdateAsync(value, ignoreEtag, cancellationToken));
		}

		return results;

	}

	public ValueTask UpdatePartialAsync(
		string id,
		Action<IPatchOperationBuilder<TEntity>> builder,
		string? etag = null,
		CancellationToken cancellationToken = default) {

		var pk = ResolvePartitionKey(id);

		var item = InMemoryStorage.GetValues<TEntity>()
			   .Select(this.DeserializeItem)
			   .FirstOrDefault(x => x.Id == id && x.PartitionKey == pk.ToString());

		switch (item) {
			case null:
				throw NotFound();
			case IEtagEntity etagEntity when
				etag != default &&
				!string.IsNullOrWhiteSpace(etag) &&
				etagEntity.Etag != etag:
				throw MismatchedEtags();
		}

		PatchOperationBuilder<TEntity> patchOperationBuilder = new();

		builder(patchOperationBuilder);

		foreach (var internalPatchOperation in
				 patchOperationBuilder._rawPatchOperations.Where(ipo => ipo.Type is PatchOperationType.Replace or PatchOperationType.Set)) {
			var property = item.GetType().GetProperty(internalPatchOperation.PropertyInfo.Name);
			property?.SetValue(item, internalPatchOperation.NewValue);
		}


		InMemoryStorage.GetDictionary<TEntity>()[id] = InMemoryRepository<TEntity>.SerializeItem(item, Guid.NewGuid().ToString(), CurrentTs);

		return ValueTask.CompletedTask;

	}


	private ValueTask<TEntity> UpdateAsync(
		TEntity value,
		bool ignoreEtag = false) {

		// Only check ETags if we're not ignoring them
		if (!ignoreEtag && value is IEtagEntity valueWithEtag) {
			if (string.IsNullOrWhiteSpace(valueWithEtag.Etag)) {
				throw new InvalidOperationException($"Cannot update entity with ID {value.Id}: Missing valid ETag");
			}
			// Try to get the existing item
			if (InMemoryStorage.GetDictionary<TEntity>().TryGetValue(value.Id, out var foundValue)) {
				// Compare ETags if the existing item has one
				if (this.DeserializeItem(foundValue) is IEtagEntity existingItemWithEtag && valueWithEtag.Etag != existingItemWithEtag.Etag) {
					MismatchedEtags();
				}
			}
		}

		InMemoryStorage.GetDictionary<TEntity>()[value.Id] = InMemoryRepository<TEntity>.SerializeItem(value, Guid.NewGuid().ToString(), CurrentTs);

		var item = this.DeserializeItem(InMemoryStorage.GetDictionary<TEntity>()[value.Id]);

		return new ValueTask<TEntity>(item);

	}

	public ValueTask<(bool Restored, TEntity Entity)> RestoreAsync(
		string id,
		CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(id);
		cancellationToken.ThrowIfCancellationRequested();

		var pk = ResolvePartitionKey(id);

		// Find the item
		var item = InMemoryStorage.GetValues<TEntity>()
			.Select(this.DeserializeItem)
			.FirstOrDefault(i => i.Id == id && new PartitionKey(i.PartitionKey) == pk)
			?? throw InMemoryRepository<TEntity>.NotFound();

		if (item is not IRestorableEntity restorable) {
			throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} does not support restore operations");
		}

		// If not deleted, return without restoring
		if (!restorable.IsDeleted) {
			return ValueTask.FromResult((false, item));
		}

		// Perform restore
		restorable.IsDeleted = false;
		restorable.DeletedBy = string.Empty;
		restorable.DeletedOn = null;
		restorable.DeletedInTimeZone = null;
		restorable.RestoreCount++;

		// Update in storage
		InMemoryStorage.GetDictionary<TEntity>()[item.Id] = SerializeItem(item);

		return ValueTask.FromResult((true, item));

	}

}