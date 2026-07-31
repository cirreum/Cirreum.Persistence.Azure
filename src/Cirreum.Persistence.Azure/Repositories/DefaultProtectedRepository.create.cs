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

		// The authorized parent and the persisted parent must be the same resource — otherwise
		// the caller could authorize against a parent they hold rights on while the entity
		// declares (and inherits the ACL of) a different parent nobody authorized.
		if (!string.Equals(value.ParentResourceId, parentResourceId, StringComparison.Ordinal)) {
			throw new ArgumentException(
				$"parentResourceId '{parentResourceId ?? "<null>"}' does not match the entity's declared " +
				$"{nameof(IProtectedResource.ParentResourceId)} '{value.ParentResourceId ?? "<null>"}'. " +
				"The parent the permission is checked against must be the parent the entity is persisted under.",
				nameof(parentResourceId));
		}

		// Check permission against the parent resource (or root defaults when null).
		var result = await this._evaluator.CheckAsync<TEntity>(parentResourceId, permission, cancellationToken)
			.ConfigureAwait(false);

		if (result.IsFailure) {
			throw result.Error;
		}

		var created = await this._repository.CreateAsync(value, cancellationToken)
			.ConfigureAwait(false);

		// Auto-populate the materialized ancestor path if the entity type supports it.
		// Returns the refreshed entity so the caller observes the persisted ancestor chain.
		return await this.TryPopulateAncestorsAsync(created, parentResourceId, cancellationToken)
			.ConfigureAwait(false);
	}

	/// <summary>
	/// Computes and patches the <see cref="IProtectedResource.AncestorResourceIds"/> on
	/// a newly created entity using a Cosmos patch operation. Returns the refreshed entity
	/// when a patch was applied; otherwise returns the entity unchanged.
	/// </summary>
	private async ValueTask<TEntity> TryPopulateAncestorsAsync(
		TEntity entity,
		string? parentResourceId,
		CancellationToken cancellationToken) {

		if (!SupportsAncestorPath() || parentResourceId is null) {
			return entity;
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

		// Patch the Cosmos document directly — works with init-only properties. The path is
		// resolved against the instance's configured naming policy / [JsonPropertyName] so the
		// patch targets the property the serializer actually wrote.
		await this._repository.UpdatePartialAsync(
			entity.Id,
			ops => ops.SetByPath(this.AncestorsJsonName, (IReadOnlyList<string>)ancestors),
			cancellationToken: cancellationToken)
			.ConfigureAwait(false);

		// Re-read so the returned entity reflects the persisted ancestor chain (properties may
		// be init-only, so the patch cannot be mirrored onto the in-memory instance).
		return await this._repository.GetAsync(entity.Id, cancellationToken: cancellationToken)
			.ConfigureAwait(false);
	}

}
