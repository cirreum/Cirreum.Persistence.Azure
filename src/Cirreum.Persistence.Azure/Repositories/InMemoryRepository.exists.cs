namespace Cirreum.Persistence;

using Cirreum.Persistence.Extensions;

sealed partial class InMemoryRepository<TEntity> {

	public ValueTask<bool> ExistsAsync(
		string id,
		bool includeDeleted = false,
		CancellationToken _ = default) {

		var pk = ResolvePartitionKey(id);
		var item = InMemoryStorage
			.GetValues<TEntity>()
			.Select(this.DeserializeItem)
			.FirstOrDefault(i => i.Id == id && new PartitionKey(i.PartitionKey) == pk);

		if (item is null) {
			return new ValueTask<bool>(false);
		}

		if (!includeDeleted
			&& typeof(IDeletableEntity).IsAssignableFrom(typeof(TEntity))
			&& item is IDeletableEntity deletable
			&& deletable.IsDeleted) {
			return new ValueTask<bool>(false);
		}

		return new ValueTask<bool>(true);
	}

	public ValueTask<bool> ExistsAsync(
		Expression<Func<TEntity, bool>> predicate,
		bool includeDeleted = false,
		CancellationToken cancellationToken = default) {

		var typeFilter = (Expression<Func<TEntity, bool>>)(item =>
			item.EntityType == typeof(TEntity).Name);

		var combinedPredicate = predicate.Compose(typeFilter, Expression.AndAlso);

		if (!includeDeleted && typeof(IDeletableEntity).IsAssignableFrom(typeof(TEntity))) {
			var deleteFilter = (Expression<Func<TEntity, bool>>)(x =>
				!((IDeletableEntity)x).IsDeleted);
			combinedPredicate = combinedPredicate.Compose(deleteFilter, Expression.AndAlso);
		}

		return new ValueTask<bool>(
			InMemoryStorage
				.GetValues<TEntity>()
				.Select(this.DeserializeItem)
				.Any(combinedPredicate.Compile())
		);
	}

}