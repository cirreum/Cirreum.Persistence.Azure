namespace Cirreum.Persistence;

using Cirreum.Persistence.Extensions;

sealed partial class InMemoryRepository<TEntity> {

	public ValueTask<CursorResult<TEntity>> PageCursorAsync(
		Expression<Func<TEntity, bool>>? predicate,
		bool includeDeleted,
		int pageSize,
		string? cursor,
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

		var allItems = filteredItems
			.Where(combinedPredicate.Compile())
			.ToList();

		// Parse cursor as an offset
		var offset = 0;
		if (!string.IsNullOrEmpty(cursor) && int.TryParse(cursor, out var parsedOffset)) {
			offset = parsedOffset;
		}

		var items = allItems
			.Skip(offset)
			.Take(pageSize)
			.ToList();

		var nextOffset = offset + items.Count;
		var hasMore = nextOffset < allItems.Count;
		var nextCursor = hasMore ? nextOffset.ToString() : null;

		return new ValueTask<CursorResult<TEntity>>(new CursorResult<TEntity>(
			items.AsReadOnly(),
			nextCursor,
			hasMore));
	}

	public ValueTask<PagedResult<TEntity>> PageAsync(
		Expression<Func<TEntity, bool>>? predicate,
		bool includeDeleted,
		int pageNumber,
		int pageSize,
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

		var allFilteredItems = filteredItems
			.Where(combinedPredicate.Compile())
			.ToList();

		var totalCount = allFilteredItems.Count;

		var items = allFilteredItems
			.Skip(pageSize * (pageNumber - 1))
			.Take(pageSize)
			.ToList();

		return new ValueTask<PagedResult<TEntity>>(new PagedResult<TEntity>(
			items.AsReadOnly(),
			totalCount,
			pageSize,
			pageNumber));
	}

	public ValueTask<SliceResult<TEntity>> SliceAsync(
		Expression<Func<TEntity, bool>>? predicate,
		bool includeDeleted,
		int count,
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

		// Fetch count + 1 to check if there are more items
		var items = filteredItems
			.Where(combinedPredicate.Compile())
			.Take(count + 1)
			.ToList();

		var hasMore = items.Count > count;
		var resultItems = hasMore ? items.Take(count).ToList().AsReadOnly() : items.AsReadOnly();

		return new ValueTask<SliceResult<TEntity>>(new SliceResult<TEntity>(resultItems, hasMore));
	}

}
