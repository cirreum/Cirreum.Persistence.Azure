namespace Cirreum.Persistence;

using Cirreum.Authorization.Resources;
using Cirreum.Exceptions;

/// <summary>
/// Optional base class for <see cref="IAccessEntryProvider{T}"/> when custom hierarchy
/// logic is needed beyond what the entity declares via <see cref="IProtectedResource"/>.
/// </summary>
/// <typeparam name="TEntity">
/// The entity type, which must implement both <see cref="IEntity"/> (persistence) and
/// <see cref="IProtectedResource"/> (embedded ACL).
/// </typeparam>
/// <remarks>
/// <para>
/// In most cases, you do not need this class. The auto-registered
/// default provider handles hierarchy walking using the
/// entity's own <see cref="IProtectedResource.ParentResourceId"/> and
/// <see cref="IProtectedResource.RootDefaults"/>. Use this base class only when the
/// hierarchy cannot be expressed directly on the entity (e.g., cross-type parent lookups
/// or computed parent relationships).
/// </para>
/// <para>
/// Registering a custom implementation will override the default provider in DI.
/// </para>
/// <para>
/// <b>Important:</b> Inject <see cref="IRepository{TEntity}"/> (the base repository),
/// not <see cref="IProtectedRepository{TEntity}"/>. The evaluator uses this provider to
/// walk the hierarchy for access resolution — injecting the protected repository would
/// create a recursive ACL check loop.
/// </para>
/// </remarks>
/// <param name="repository">
/// The base repository used to load resources by ID during hierarchy walking.
/// </param>
public abstract class RepositoryAccessEntryProvider<TEntity>(
	IRepository<TEntity> repository
) : IAccessEntryProvider<TEntity>
	where TEntity : IEntity, IProtectedResource {

	/// <inheritdoc/>
	public async ValueTask<TEntity?> GetByIdAsync(
		string resourceId,
		CancellationToken cancellationToken) {
		try {
			return await repository.GetAsync(resourceId, cancellationToken).ConfigureAwait(false);
		} catch (NotFoundException) {
			return default;
		}
	}

	/// <inheritdoc/>
	public abstract string? GetParentId(TEntity resource);

	/// <inheritdoc/>
	public abstract IReadOnlyList<AccessEntry> RootDefaults { get; }

	/// <inheritdoc/>
	public virtual async ValueTask<IReadOnlyList<TEntity>> GetManyByIdAsync(
		IReadOnlyList<string> resourceIds,
		CancellationToken cancellationToken) {

		if (resourceIds.Count == 0) {
			return [];
		}

		return await repository.GetManyAsync(resourceIds, cancellationToken: cancellationToken)
			.ConfigureAwait(false);
	}
}
