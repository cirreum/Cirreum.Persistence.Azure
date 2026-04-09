namespace Cirreum.Persistence;

using System.Runtime.CompilerServices;

/// <summary>
/// Delegated <see cref="IRepository{TEntity}"/> members — pass through to the inner repository
/// without ACL checks.
/// </summary>
sealed partial class DefaultProtectedRepository<TEntity> {

	// ———————————————————————— IReadOnlyRepository ————————————————————————

	public ValueTask<TEntity> GetAsync(
		string id,
		bool includeDeleted,
		CancellationToken cancellationToken = default) =>
		this._repository.GetAsync(id, includeDeleted, cancellationToken);

	public ValueTask<IReadOnlyList<TEntity>> GetManyAsync(
		IEnumerable<string> ids,
		bool includeDeleted,
		CancellationToken cancellationToken = default) =>
		this._repository.GetManyAsync(ids, includeDeleted, cancellationToken);

	public ValueTask<IReadOnlyList<TEntity>> GetAllAsync(
		bool includeDeleted,
		CancellationToken cancellationToken = default) =>
		this._repository.GetAllAsync(includeDeleted, cancellationToken);

	public ValueTask<TEntity?> FirstOrNullAsync(
		Expression<Func<TEntity, bool>> predicate,
		bool includeDeleted,
		CancellationToken cancellationToken = default) =>
		this._repository.FirstOrNullAsync(predicate, includeDeleted, cancellationToken);

	public ValueTask<IReadOnlyList<TEntity>> QueryAsync(
		Expression<Func<TEntity, bool>> predicate,
		bool includeDeleted,
		CancellationToken cancellationToken = default) =>
		this._repository.QueryAsync(predicate, includeDeleted, cancellationToken);

	public ValueTask<IReadOnlyList<TEntity>> QueryAsync(
		string query,
		CancellationToken cancellationToken = default) =>
		this._repository.QueryAsync(query, cancellationToken);

	public ValueTask<IReadOnlyList<TEntity>> QueryAsync(
		string parameterizedQuery,
		IEnumerable<KeyValuePair<string, string>> parameters,
		CancellationToken cancellationToken = default) =>
		this._repository.QueryAsync(parameterizedQuery, parameters, cancellationToken);

	public IAsyncEnumerable<TEntity> SequenceQueryAsync(
		Expression<Func<TEntity, bool>> predicate,
		bool includeDeleted,
		int? maxResults = null,
		CancellationToken cancellationToken = default) =>
		this._repository.SequenceQueryAsync(predicate, includeDeleted, maxResults, cancellationToken);

	public ValueTask<bool> ExistsAsync(
		string id,
		bool includeDeleted,
		CancellationToken cancellationToken = default) =>
		this._repository.ExistsAsync(id, includeDeleted, cancellationToken);

	public ValueTask<bool> ExistsAsync(
		Expression<Func<TEntity, bool>> predicate,
		bool includeDeleted,
		CancellationToken cancellationToken = default) =>
		this._repository.ExistsAsync(predicate, includeDeleted, cancellationToken);

	public ValueTask<int> CountAsync(
		bool includeDeleted,
		CancellationToken cancellationToken = default) =>
		this._repository.CountAsync(includeDeleted, cancellationToken);

	public ValueTask<int> CountAsync(
		Expression<Func<TEntity, bool>> predicate,
		bool includeDeleted,
		CancellationToken cancellationToken = default) =>
		this._repository.CountAsync(predicate, includeDeleted, cancellationToken);

	public ValueTask<CursorResult<TEntity>> PageCursorAsync(
		Expression<Func<TEntity, bool>>? predicate,
		bool includeDeleted,
		int pageSize,
		string? cursor,
		CancellationToken cancellationToken = default) =>
		this._repository.PageCursorAsync(predicate, includeDeleted, pageSize, cursor, cancellationToken);

	public ValueTask<PagedResult<TEntity>> PageAsync(
		Expression<Func<TEntity, bool>>? predicate,
		bool includeDeleted,
		int pageNumber,
		int pageSize,
		CancellationToken cancellationToken = default) =>
		this._repository.PageAsync(predicate, includeDeleted, pageNumber, pageSize, cancellationToken);

	public ValueTask<SliceResult<TEntity>> SliceAsync(
		Expression<Func<TEntity, bool>>? predicate,
		bool includeDeleted,
		int count,
		CancellationToken cancellationToken = default) =>
		this._repository.SliceAsync(predicate, includeDeleted, count, cancellationToken);

	// ———————————————————————— IWriteOnlyRepository ————————————————————————

	public ValueTask<TEntity> CreateAsync(
		TEntity value,
		CancellationToken cancellationToken = default) =>
		this._repository.CreateAsync(value, cancellationToken);

	public ValueTask<IEnumerable<TEntity>> CreateAsync(
		IEnumerable<TEntity> values,
		CancellationToken cancellationToken = default) =>
		this._repository.CreateAsync(values, cancellationToken);

	public ValueTask<TEntity> UpdateAsync(
		TEntity value,
		CancellationToken cancellationToken = default) =>
		this._repository.UpdateAsync(value, cancellationToken);

	public ValueTask<TEntity> UpdateAsync(
		TEntity value,
		bool ignoreEtag,
		CancellationToken cancellationToken = default) =>
		this._repository.UpdateAsync(value, ignoreEtag, cancellationToken);

	public ValueTask<IEnumerable<TEntity>> UpdateAsync(
		IEnumerable<TEntity> values,
		bool ignoreEtag,
		CancellationToken cancellationToken = default) =>
		this._repository.UpdateAsync(values, ignoreEtag, cancellationToken);

	public ValueTask UpdatePartialAsync(
		string id,
		Action<IPatchOperationBuilder<TEntity>> operations,
		string? concurrencyToken = default,
		CancellationToken cancellationToken = default) =>
		this._repository.UpdatePartialAsync(id, operations, concurrencyToken, cancellationToken);

	public ValueTask<(bool Restored, TEntity Entity)> RestoreAsync(
		string id,
		CancellationToken cancellationToken = default) =>
		this._repository.RestoreAsync(id, cancellationToken);

	public ValueTask DeleteAsync(
		TEntity value,
		CancellationToken cancellationToken = default) =>
		this._repository.DeleteAsync(value, cancellationToken);

	public ValueTask DeleteAsync(
		string id,
		CancellationToken cancellationToken = default) =>
		this._repository.DeleteAsync(id, cancellationToken);

	public ValueTask<TEntity> DeleteAsync(
		TEntity value,
		CancellationToken cancellationToken,
		bool softDelete = true) =>
		this._repository.DeleteAsync(value, cancellationToken, softDelete);

	public ValueTask<TEntity> DeleteAsync(
		string id,
		CancellationToken cancellationToken,
		bool softDelete = true) =>
		this._repository.DeleteAsync(id, cancellationToken, softDelete);

	// ———————————————————————— IBatchRepository ————————————————————————

	public ValueTask CreateAsBatchAsync(
		IEnumerable<TEntity> items,
		CancellationToken cancellationToken = default) =>
		this._repository.CreateAsBatchAsync(items, cancellationToken);

	public ValueTask UpdateAsBatchAsync(
		IEnumerable<TEntity> items,
		CancellationToken cancellationToken = default) =>
		this._repository.UpdateAsBatchAsync(items, cancellationToken);

	public ValueTask DeleteAsBatchAsync(
		IEnumerable<TEntity> items,
		CancellationToken cancellationToken = default) =>
		this._repository.DeleteAsBatchAsync(items, cancellationToken);

	public ValueTask DeleteAsBatchAsync(
		IEnumerable<TEntity> items,
		CancellationToken cancellationToken = default,
		bool softDelete = true) =>
		this._repository.DeleteAsBatchAsync(items, cancellationToken, softDelete);

	public ValueTask<IEnumerable<(string Id, bool Restored)>> RestoreAsBatchAsync(
		IEnumerable<TEntity> items,
		CancellationToken cancellationToken = default) =>
		this._repository.RestoreAsBatchAsync(items, cancellationToken);

}
