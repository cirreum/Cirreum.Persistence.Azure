namespace Cirreum.Persistence;

using Cirreum.Authorization.Resources;
using Permission = Cirreum.Authorization.Permission;

/// <summary>
/// Permission-aware delete operations.
/// </summary>
sealed partial class DefaultProtectedRepository<TEntity> {

	/// <inheritdoc/>
	public async ValueTask<TEntity> DeleteAsync(
		TEntity value,
		Permission permission,
		CancellationToken cancellationToken = default) {

		var result = await this._evaluator.CheckAsync(value, permission, cancellationToken)
			.ConfigureAwait(false);

		if (result.IsFailure) {
			throw result.Error;
		}

		// Delegate to the soft-delete overload (returns the entity).
		return await this._repository.DeleteAsync(value, cancellationToken, softDelete: true)
			.ConfigureAwait(false);
	}

	/// <inheritdoc/>
	public async ValueTask<TEntity> DeleteAsync(
		string id,
		Permission permission,
		CancellationToken cancellationToken = default) {

		// Load the entity first for the ACL check, then pass it to the entity-based
		// delete to avoid a redundant second read inside the base repository.
		var entity = await this._repository.GetAsync(id, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		return await this.DeleteAsync(entity, permission, cancellationToken)
			.ConfigureAwait(false);
	}

}
