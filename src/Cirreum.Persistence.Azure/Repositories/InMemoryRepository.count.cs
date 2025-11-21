namespace Cirreum.Persistence;

using Cirreum.Persistence.Extensions;
using System.Linq.Expressions;

sealed partial class InMemoryRepository<TEntity> {

	public ValueTask<int> CountAsync(
		bool includeDeleted = false,
		CancellationToken _ = default) {

		var query = InMemoryStorage.GetValues<TEntity>()
			.Select(this.DeserializeItem)
			.Where(item => item.EntityType == typeof(TEntity).Name);

		if (!includeDeleted && typeof(IDeletableEntity).IsAssignableFrom(typeof(TEntity))) {
			query = query.Where(x => !((IDeletableEntity)x).IsDeleted);
		}

		return new ValueTask<int>(query.Count());
	}

	public ValueTask<int> CountAsync(
		Expression<Func<TEntity, bool>> predicate,
		bool includeDeleted = false,
		CancellationToken _ = default) {

		var typeFilter = (Expression<Func<TEntity, bool>>)(item =>
			item.EntityType == typeof(TEntity).Name);

		var combinedPredicate = predicate.Compose(typeFilter, Expression.AndAlso);

		if (!includeDeleted && typeof(IDeletableEntity).IsAssignableFrom(typeof(TEntity))) {
			var deleteFilter = (Expression<Func<TEntity, bool>>)(x =>
				!((IDeletableEntity)x).IsDeleted);
			combinedPredicate = combinedPredicate.Compose(deleteFilter, Expression.AndAlso);
		}

		return new ValueTask<int>(
			InMemoryStorage.GetValues<TEntity>()
				.Select(this.DeserializeItem)
				.Count(combinedPredicate.Compile())
		);

	}

}