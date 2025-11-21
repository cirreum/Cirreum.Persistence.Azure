namespace Cirreum.Persistence;

using Cirreum.Persistence.Extensions;

sealed partial class InMemoryRepository<TEntity> {

	public ValueTask<IContinuationPage<TEntity>> PageContinuationAsync(
		Expression<Func<TEntity, bool>>? predicate = null,
		bool includeDeleted = false,
		int pageSize = 25,
		string? continuationToken = null,
		CancellationToken cancellationToken = default) {
		throw new NotImplementedException();
	}

	public ValueTask<IOffSetPage<TEntity>> PageOffsetAsync(
		Expression<Func<TEntity, bool>>? predicate = null,
		bool includeDeleted = false,
		int pageNumber = 1,
		int pageSize = 25,
		bool includeTotalCount = false,
		CancellationToken cancellationToken = default) {

		var filteredItems = InMemoryStorage
			.GetValues<TEntity>()
			.Select(this.DeserializeItem);

		var typeFilter = (Expression<Func<TEntity, bool>>)(item =>
			item.EntityType == typeof(TEntity).Name);

		var combinedPredicate = predicate?.Compose(typeFilter, Expression.AndAlso) ?? typeFilter;

		if (!includeDeleted && typeof(IDeletableEntity).IsAssignableFrom(typeof(TEntity))) {
			var deleteFilter = (Expression<Func<TEntity, bool>>)(x =>
				!((IDeletableEntity)x).IsDeleted);
			combinedPredicate = combinedPredicate.Compose(deleteFilter, Expression.AndAlso);
		}

		var items = filteredItems
			.Where(combinedPredicate.Compile())
			.Skip(pageSize * (pageNumber - 1))
			.Take(pageSize)
			.ToList();

		return new ValueTask<IOffSetPage<TEntity>>(new OffsetPage<TEntity>(
			includeTotalCount ? items.Count : -1,
			pageNumber,
			pageSize,
			items.AsReadOnly(),
			0));
	}

}