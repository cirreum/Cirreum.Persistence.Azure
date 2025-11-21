namespace Cirreum.Persistence;

sealed partial class InMemoryRepository<TEntity> {

	public async ValueTask<TEntity> CreateAsync(
		TEntity value,
		CancellationToken cancellationToken = default) =>
		await this.CreateAsync(value);

	public async ValueTask<IEnumerable<TEntity>> CreateAsync(
		IEnumerable<TEntity> values,
		CancellationToken cancellationToken = default) {

		List<TEntity> results = [];

		foreach (var value in values) {
			var item = await this.CreateAsync(value, cancellationToken);
			results.Add(item);
		}

		return results;

	}

	private async Task<TEntity> CreateAsync(TEntity value) {

		value.Id = GenerateItemId(value);

		if (value is IAuditableEntity auditable) {
			auditable.CreatedInTimeZone = this._datetimeService.LocalTimeZoneId;
			auditable.CreatedOn = this._datetimeService.UtcOffset;
			var user = await this._userAccessor.GetUser();
			var userName = user.Name;
			auditable.CreatedBy = userName;
		}

		// Initialize soft-delete properties
		if (value is IDeletableEntity deletable) {
			deletable.IsDeleted = false;
			deletable.DeletedBy = string.Empty;
			deletable.DeletedOn = null;
			deletable.DeletedInTimeZone = null;

			// If it's also restorable, initialize restore count
			if (value is IRestorableEntity restorable) {
				restorable.RestoreCount = 0;
			}
		}

		var oldItem = InMemoryStorage.GetValues<TEntity>()
			.Select(this.DeserializeItem)
			.FirstOrDefault(i => i.Id == value.Id && i.PartitionKey == value.PartitionKey);

		if (oldItem is not null) {
			throw InMemoryRepository<TEntity>.Conflict();
		}

		var serializedValue = InMemoryRepository<TEntity>.SerializeItem(value, Guid.NewGuid().ToString(), CurrentTs);
		InMemoryStorage.GetDictionary<TEntity>().TryAdd(value.Id, serializedValue);

		value = this.DeserializeItem(InMemoryStorage.GetDictionary<TEntity>()[value.Id]);

		return value;

	}

}
