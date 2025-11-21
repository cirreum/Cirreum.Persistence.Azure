namespace Cirreum.Persistence;

sealed partial class InMemoryRepository<TEntity> {

	public async ValueTask<TEntity> DeleteAsync(
		TEntity value,
		CancellationToken cancellationToken = default,
		bool softDelete = true) {
		cancellationToken.ThrowIfCancellationRequested();

		ValidatePartitionKey(value);

		if (softDelete) {
			if (!typeof(IDeletableEntity).IsAssignableFrom(typeof(TEntity))) {
				throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} does not support soft delete");
			}

			if (value is IDeletableEntity deletable) {
				var user = await this._userAccessor.GetUser();
				deletable.DeletedBy = user.Name;
				deletable.DeletedOn = this._datetimeService.UtcNow;
				deletable.DeletedInTimeZone = this._datetimeService.LocalTimeZoneId;
				deletable.IsDeleted = true;

				// Update in storage
				InMemoryStorage.GetDictionary<TEntity>()[value.Id] = SerializeItem(value);
				return value;
			} else {
				throw new InvalidOperationException($"Entity with id {value.Id} does not support soft delete");
			}
		}

		// Hard delete
		if (!InMemoryStorage.GetDictionary<TEntity>().TryRemove(value.Id, out _)) {
			throw InMemoryRepository<TEntity>.NotFound();
		}

		return value;

	}

	public ValueTask<TEntity> DeleteAsync(
		string id,
		CancellationToken cancellationToken = default,
		bool softDelete = true) {
		ArgumentException.ThrowIfNullOrWhiteSpace(id);
		cancellationToken.ThrowIfCancellationRequested();

		var pk = ResolvePartitionKey(id);

		if (softDelete) {
			if (!typeof(IDeletableEntity).IsAssignableFrom(typeof(TEntity))) {
				throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} does not support soft delete");
			}

			// Find and update the item
			var item = InMemoryStorage.GetValues<TEntity>()
				.Select(this.DeserializeItem)
				.FirstOrDefault(i => i.Id == id && new PartitionKey(i.PartitionKey) == pk)
				?? throw InMemoryRepository<TEntity>.NotFound();

			return this.DeleteAsync(item, cancellationToken, true);
		}

		// Hard delete
		if (!InMemoryStorage.GetDictionary<TEntity>().TryRemove(id, out var serializedItem)) {
			throw InMemoryRepository<TEntity>.NotFound();
		}

		return ValueTask.FromResult(this.DeserializeItem(serializedItem));
	}

	// Simple overloads that default to soft delete
	public async ValueTask DeleteAsync(
		TEntity value,
		CancellationToken cancellationToken = default) {
		await this.DeleteAsync(value, cancellationToken, true);
	}

	public async ValueTask DeleteAsync(
		string id,
		CancellationToken cancellationToken = default) {
		await this.DeleteAsync(id, cancellationToken, true);
	}

}