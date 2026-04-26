namespace Cirreum.Persistence;

using Permission = Cirreum.Authorization.Permission;

/// <summary>
/// Permission-aware query operations.
/// </summary>
sealed partial class DefaultProtectedRepository<TEntity> {

	/// <inheritdoc/>
	public async ValueTask<IReadOnlyList<TEntity>> QueryAsync(
		Expression<Func<TEntity, bool>> predicate,
		Permission permission,
		CancellationToken cancellationToken = default) {

		var entities = await this._repository.QueryAsync(predicate, false, cancellationToken)
			.ConfigureAwait(false);

		return await this._evaluator.FilterAsync(entities, permission, cancellationToken)
			.ConfigureAwait(false);
	}

	/// <inheritdoc/>
	public async ValueTask<TEntity?> FirstOrNullAsync(
		Expression<Func<TEntity, bool>> predicate,
		Permission permission,
		CancellationToken cancellationToken = default) {

		// Query all matches and filter by permission, then take the first.
		// We can't short-circuit at the DB level because ACL filtering is post-query.
		var entities = await this._repository.QueryAsync(predicate, false, cancellationToken)
			.ConfigureAwait(false);

		var authorized = await this._evaluator.FilterAsync(entities, permission, cancellationToken)
			.ConfigureAwait(false);

		return authorized.Count > 0 ? authorized[0] : default;
	}

}
