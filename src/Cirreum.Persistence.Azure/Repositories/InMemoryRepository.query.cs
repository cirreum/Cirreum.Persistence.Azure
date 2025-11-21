namespace Cirreum.Persistence;

using Cirreum.Persistence.Extensions;
using System.Runtime.CompilerServices;

sealed partial class InMemoryRepository<TEntity> {

	public ValueTask<TEntity?> FirstOrNullAsync(
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

		return new ValueTask<TEntity?>(
			InMemoryStorage.GetValues<TEntity>()
				.Select(this.DeserializeItem)
				.FirstOrDefault(combinedPredicate.Compile()));
	}

	public ValueTask<IReadOnlyList<TEntity>> QueryAsync(
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

		return new ValueTask<IReadOnlyList<TEntity>>(
			InMemoryStorage.GetValues<TEntity>()
				.Select(this.DeserializeItem)
				.Where(combinedPredicate.Compile())
				.ToList()
				.AsReadOnly());
	}

	public ValueTask<IReadOnlyList<TEntity>> QueryAsync(
		string query,
		CancellationToken cancellationToken = default) {
		throw new NotImplementedException();
	}

	public ValueTask<IReadOnlyList<TEntity>> QueryAsync(
		string parametereizedQuery,
		IEnumerable<KeyValuePair<string, string>> parameters,
		CancellationToken cancellationToken = default) {
		throw new NotImplementedException();
	}

	public async IAsyncEnumerable<TEntity> SequenceQueryAsync(
		Expression<Func<TEntity, bool>> predicate,
		bool includeDeleted = false,
		int? maxResults = null,
		[EnumeratorCancellation] CancellationToken cancellationToken = default) {

		var typeFilter = (Expression<Func<TEntity, bool>>)(item =>
			item.EntityType == typeof(TEntity).Name);

		var combinedPredicate = predicate.Compose(typeFilter, Expression.AndAlso);

		if (!includeDeleted && typeof(IDeletableEntity).IsAssignableFrom(typeof(TEntity))) {
			var deleteFilter = (Expression<Func<TEntity, bool>>)(x =>
				!((IDeletableEntity)x).IsDeleted);
			combinedPredicate = combinedPredicate.Compose(deleteFilter, Expression.AndAlso);
		}

		var query = InMemoryStorage.GetValues<TEntity>()
			.Select(this.DeserializeItem)
			.Where(combinedPredicate.Compile());

		if (maxResults.HasValue) {
			query = query.Take(maxResults.Value);
		}

		foreach (var item in query) {
			if (cancellationToken.IsCancellationRequested) {
				yield break;
			}

			yield return item;
		}

		await Task.CompletedTask;
	}

}