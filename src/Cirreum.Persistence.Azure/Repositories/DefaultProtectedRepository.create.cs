namespace Cirreum.Persistence;

using Cirreum.Authorization.Resources;
using Permission = Cirreum.Authorization.Permission;

/// <summary>
/// Permission-aware create operations.
/// </summary>
sealed partial class DefaultProtectedRepository<TEntity> {

	/// <inheritdoc/>
	public async ValueTask<TEntity> CreateAsync(
		TEntity value,
		string? parentResourceId,
		Permission permission,
		CancellationToken cancellationToken = default) {

		// Check permission against the parent resource (or root defaults when null).
		var result = await this._evaluator.CheckAsync<TEntity>(parentResourceId, permission, cancellationToken)
			.ConfigureAwait(false);

		if (result.IsFailure) {
			throw result.Error;
		}

		var created = await this._repository.CreateAsync(value, cancellationToken)
			.ConfigureAwait(false);

		// Auto-populate the materialized ancestor path if the entity type supports it.
		await this.TryPopulateAncestorsAsync(created, parentResourceId, cancellationToken)
			.ConfigureAwait(false);

		return created;
	}

	/// <summary>
	/// Computes and patches the <see cref="IProtectedResource.AncestorResourceIds"/> on
	/// a newly created entity using a Cosmos patch operation.
	/// </summary>
	private async ValueTask TryPopulateAncestorsAsync(
		TEntity entity,
		string? parentResourceId,
		CancellationToken cancellationToken) {

		if (!SupportsAncestorPath() || parentResourceId is null) {
			return;
		}

		// Build ancestor chain: [parentId, ...parent.AncestorResourceIds]
		var parent = await this._repository.GetAsync(parentResourceId, cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		var ancestors = new List<string> { parentResourceId };

		if (parent.AncestorResourceIds is { Count: > 0 } parentAncestors) {
			ancestors.AddRange(parentAncestors);
		} else {
			// Migration scenario: parent was created before ancestor path feature.
			// Walk up manually to build the full chain.
			var visited = new HashSet<string>(StringComparer.Ordinal) { parentResourceId };
			var currentId = parent.ParentResourceId;
			while (currentId is not null && visited.Add(currentId)) {
				ancestors.Add(currentId);
				try {
					var current = await this._repository.GetAsync(currentId, cancellationToken: cancellationToken)
						.ConfigureAwait(false);
					currentId = current.ParentResourceId;
				} catch (Cirreum.Exceptions.NotFoundException) {
					break; // Orphan — stop walking
				}
			}
		}

		// Patch the Cosmos document directly — works with init-only properties.
		await this._repository.UpdatePartialAsync(
			entity.Id,
			ops => ops.SetByPath("ancestorResourceIds", (IReadOnlyList<string>)ancestors),
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);
	}

}
