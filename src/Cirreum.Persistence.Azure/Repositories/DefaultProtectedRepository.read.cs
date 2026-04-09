namespace Cirreum.Persistence;

using Cirreum.Authorization.Resources;
using Permission = Cirreum.Authorization.Permission;

/// <summary>
/// Permission-aware read operations.
/// </summary>
sealed partial class DefaultProtectedRepository<TEntity> {

	/// <inheritdoc/>
	public async ValueTask<TEntity> GetAsync(
		string id,
		Permission permission,
		CancellationToken cancellationToken = default) {

		var entity = await this._repository.GetAsync(id, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var result = await this._evaluator.CheckAsync(entity, permission, cancellationToken)
			.ConfigureAwait(false);

		if (result.IsFailure) {
			throw result.Error;
		}

		return entity;
	}

	/// <inheritdoc/>
	public async ValueTask<IReadOnlyList<TEntity>> GetManyAsync(
		IEnumerable<string> ids,
		Permission permission,
		CancellationToken cancellationToken = default) {

		var entities = await this._repository.GetManyAsync(ids, false, cancellationToken)
			.ConfigureAwait(false);

		return await this._evaluator.FilterAsync(entities, permission, cancellationToken)
			.ConfigureAwait(false);
	}

	/// <inheritdoc/>
	public async ValueTask<IReadOnlyList<TEntity>> GetAllAsync(
		Permission permission,
		CancellationToken cancellationToken = default) {

		var entities = await this._repository.GetAllAsync(false, cancellationToken)
			.ConfigureAwait(false);

		return await this._evaluator.FilterAsync(entities, permission, cancellationToken)
			.ConfigureAwait(false);
	}

}
