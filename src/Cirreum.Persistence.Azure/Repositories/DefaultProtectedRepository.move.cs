namespace Cirreum.Persistence;

using Cirreum.Authorization.Resources;
using Permission = Cirreum.Authorization.Permission;

/// <summary>
/// Permission-aware move/reparent operations with ancestor chain cascade.
/// </summary>
sealed partial class DefaultProtectedRepository<TEntity> {

	/// <inheritdoc/>
	public async ValueTask MoveAsync(
		TEntity value,
		string? newParentResourceId,
		Permission permission,
		CancellationToken cancellationToken = default) {

		if (!SupportsAncestorPath()) {
			throw new InvalidOperationException(
				$"{typeof(TEntity).Name} does not support materialized ancestor paths. " +
				$"Implement {nameof(IProtectedResource.AncestorResourceIds)} on the entity to enable move operations.");
		}

		// Check permission on the entity being moved
		var entityCheck = await this._evaluator.CheckAsync(value, permission, cancellationToken)
			.ConfigureAwait(false);
		if (entityCheck.IsFailure) {
			throw entityCheck.Error;
		}

		// Check permission on the new parent (or root defaults when null)
		var parentCheck = await this._evaluator.CheckAsync<TEntity>(newParentResourceId, permission, cancellationToken)
			.ConfigureAwait(false);
		if (parentCheck.IsFailure) {
			throw parentCheck.Error;
		}

		// Compute the new ancestor chain
		List<string> newAncestors;
		if (newParentResourceId is null) {
			newAncestors = [];
		} else {
			var newParent = await this._repository.GetAsync(newParentResourceId, cancellationToken: cancellationToken)
				.ConfigureAwait(false);
			newAncestors = [newParentResourceId, .. newParent.AncestorResourceIds];
		}

		// Cycle detection: ensure the entity is not becoming its own ancestor
		if (value.ResourceId is not null && newAncestors.Contains(value.ResourceId, StringComparer.Ordinal)) {
			throw new InvalidOperationException(
				$"Cannot move {typeof(TEntity).Name} '{value.ResourceId}' under '{newParentResourceId}': would create a cycle.");
		}

		// Patch the entity's parentResourceId and ancestorResourceIds
		await this._repository.UpdatePartialAsync(
			value.Id,
			ops => ops
				.SetByPath("parentResourceId", newParentResourceId)
				.SetByPath("ancestorResourceIds", (IReadOnlyList<string>)newAncestors),
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		// Cascade ancestor updates to all descendants
		if (value.ResourceId is not null) {
			await this.CascadeAncestorUpdateAsync(value.ResourceId, newAncestors, cancellationToken)
				.ConfigureAwait(false);
		}
	}

	/// <summary>
	/// Finds all descendants of the moved entity and recomputes their ancestor chains.
	/// </summary>
	private async ValueTask CascadeAncestorUpdateAsync(
		string movedEntityResourceId,
		List<string> movedEntityNewAncestors,
		CancellationToken cancellationToken) {

		// Query descendants: entities that have the moved entity in their ancestor chain.
		var descendants = await this._repository.QueryAsync(
			"SELECT * FROM c WHERE ARRAY_CONTAINS(c.ancestorResourceIds, @entityId)",
			[new KeyValuePair<string, string>("@entityId", movedEntityResourceId)],
			cancellationToken)
			.ConfigureAwait(false);

		if (descendants.Count == 0) {
			return;
		}

		// The moved entity's new full path (including itself) replaces everything
		// from the moved entity onward in each descendant's ancestor chain.
		var newTail = new List<string>(movedEntityNewAncestors.Count + 1) { movedEntityResourceId };
		newTail.AddRange(movedEntityNewAncestors);

		foreach (var descendant in descendants) {
			cancellationToken.ThrowIfCancellationRequested();

			var currentAncestors = descendant.AncestorResourceIds;
			var pivotIndex = -1;

			for (var i = 0; i < currentAncestors.Count; i++) {
				if (string.Equals(currentAncestors[i], movedEntityResourceId, StringComparison.Ordinal)) {
					pivotIndex = i;
					break;
				}
			}

			if (pivotIndex < 0) {
				continue; // Shouldn't happen, but defensive
			}

			// Keep ancestors before the moved entity, replace from the moved entity onward
			var updatedAncestors = new List<string>(pivotIndex + newTail.Count);
			for (var i = 0; i < pivotIndex; i++) {
				updatedAncestors.Add(currentAncestors[i]);
			}
			updatedAncestors.AddRange(newTail);

			await this._repository.UpdatePartialAsync(
				descendant.Id,
				ops => ops.SetByPath("ancestorResourceIds", (IReadOnlyList<string>)updatedAncestors),
				cancellationToken: cancellationToken)
				.ConfigureAwait(false);
		}
	}

}
