namespace Cirreum.Persistence;

using Cirreum.Authorization.Resources;
using Permission = Cirreum.Authorization.Permission;

/// <summary>
/// Permission-aware update operations.
/// </summary>
sealed partial class DefaultProtectedRepository<TEntity> {

	/// <inheritdoc/>
	public async ValueTask<TEntity> UpdateAsync(
		TEntity value,
		Permission permission,
		CancellationToken cancellationToken = default) {

		var result = await this._evaluator.CheckAsync(value, permission, cancellationToken)
			.ConfigureAwait(false);

		if (result.IsFailure) {
			throw result.Error;
		}

		return await this._repository.UpdateAsync(value, cancellationToken)
			.ConfigureAwait(false);
	}

}
