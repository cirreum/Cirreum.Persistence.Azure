namespace Cirreum.Persistence.Internal.Providers;

using Cirreum.Authorization.Resources;
using Cirreum.Exceptions;

/// <summary>
/// Default <see cref="IAccessEntryProvider{T}"/> that resolves everything from the entity type
/// and the base <see cref="IRepository{TEntity}"/>. No app-specific subclass required.
/// </summary>
/// <typeparam name="TEntity">
/// The entity type, which must implement both <see cref="IEntity"/> and <see cref="IProtectedResource"/>.
/// </typeparam>
/// <remarks>
/// <para>
/// <b>Hierarchy navigation:</b> Uses <see cref="IProtectedResource.ParentResourceId"/> —
/// the entity declares its own parent.
/// </para>
/// <para>
/// <b>Root defaults:</b> Reads <c>TEntity.RootDefaults</c> via the
/// <see cref="IProtectedResource"/> static virtual member. The entity type declares its
/// own root-level ACL.
/// </para>
/// <para>
/// <b>Lookups:</b> Delegates to <see cref="IRepository{TEntity}"/> for loading resources
/// by ID during the evaluator's hierarchy walk.
/// </para>
/// <para>
/// For advanced scenarios where the hierarchy shape differs from the entity's
/// <see cref="IProtectedResource.ParentResourceId"/> (e.g., cross-entity hierarchies),
/// use <see cref="RepositoryAccessEntryProvider{TEntity}"/> as an explicit override.
/// </para>
/// </remarks>
internal sealed class DefaultAccessEntryProvider<TEntity>(
	IRepository<TEntity> repository
) : IAccessEntryProvider<TEntity>
	where TEntity : IEntity, IProtectedResource {

	/// <inheritdoc/>
	public async ValueTask<TEntity?> GetByIdAsync(
		string resourceId,
		CancellationToken cancellationToken) {
		try {
			return await repository.GetAsync(resourceId, cancellationToken: cancellationToken)
				.ConfigureAwait(false);
		} catch (NotFoundException) {
			return default;
		}
	}

	/// <inheritdoc/>
	public async ValueTask<IReadOnlyList<TEntity>> GetManyByIdAsync(
		IReadOnlyList<string> resourceIds,
		CancellationToken cancellationToken) {

		if (resourceIds.Count == 0) {
			return [];
		}

		return await repository.GetManyAsync(resourceIds, cancellationToken: cancellationToken)
			.ConfigureAwait(false);
	}
}
