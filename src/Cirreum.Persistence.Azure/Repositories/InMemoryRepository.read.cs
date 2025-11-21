namespace Cirreum.Persistence;

sealed partial class InMemoryRepository<TEntity> {

	public ValueTask<TEntity> GetAsync(
		string id,
		bool includeDeleted = false,
		CancellationToken cancellationToken = default) {

		var partitionKey = ResolvePartitionKey(id);

		var item = InMemoryStorage.GetValues<TEntity>()
			.Select(this.DeserializeItem)
			.FirstOrDefault(i => i.Id == id && new PartitionKey(i.PartitionKey) == partitionKey)
			?? throw InMemoryRepository<TEntity>.NotFound();

		if (!includeDeleted
			&& typeof(IDeletableEntity).IsAssignableFrom(typeof(TEntity))
			&& item is IDeletableEntity deletable
			&& deletable.IsDeleted) {
			throw InMemoryRepository<TEntity>.NotFound();
		}

		return new ValueTask<TEntity>(item);

	}

	public ValueTask<IReadOnlyList<TEntity>> GetManyAsync(
		IEnumerable<string> ids,
		bool includeDeleted = false,
		CancellationToken cancellationToken = default) {

		var items = new List<TEntity>();
		foreach (var id in ids) {
			var partitionKey = ResolvePartitionKey(id);
			var item = InMemoryStorage.GetValues<TEntity>()
				.Select(this.DeserializeItem)
				.FirstOrDefault(i => i.Id == id && new PartitionKey(i.PartitionKey) == partitionKey);

			if (item != null) {
				if (includeDeleted ||
					!typeof(IDeletableEntity).IsAssignableFrom(typeof(TEntity)) ||
					item is not IDeletableEntity deletable ||
					!deletable.IsDeleted) {
					items.Add(item);
				}
			}
		}

		return new ValueTask<IReadOnlyList<TEntity>>(items.AsReadOnly());
	}

	public ValueTask<IReadOnlyList<TEntity>> GetAllAsync(
		bool includeDeleted = false,
		CancellationToken cancellationToken = default) {
		var query = InMemoryStorage.GetValues<TEntity>()
			.Select(this.DeserializeItem)
			.Where(item => item.EntityType == typeof(TEntity).Name);

		if (!includeDeleted && typeof(IDeletableEntity).IsAssignableFrom(typeof(TEntity))) {
			query = query.Where(x => !((IDeletableEntity)x).IsDeleted);
		}

		return new ValueTask<IReadOnlyList<TEntity>>(query.ToList().AsReadOnly());
	}

}