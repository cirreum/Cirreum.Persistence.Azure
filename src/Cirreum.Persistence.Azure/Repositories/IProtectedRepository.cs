namespace Cirreum.Persistence;

using Cirreum.Authorization;
using Cirreum.Authorization.Resources;
using Permission = Cirreum.Authorization.Permission;

/// <summary>
/// A repository interface for entities that carry embedded ACLs via <see cref="IProtectedResource"/>.
/// Extends <see cref="IRepository{TEntity}"/> with permission-aware overloads that automatically
/// evaluate object-level access using <see cref="IResourceAccessEvaluator"/>.
/// </summary>
/// <typeparam name="TEntity">
/// The entity type, which must implement both <see cref="IEntity"/> (persistence) and
/// <see cref="IProtectedResource"/> (embedded ACL).
/// </typeparam>
/// <remarks>
/// <para>
/// <b>Read (single)</b> operations throw when the caller lacks the required permission.
/// </para>
/// <para>
/// <b>Read (batch)</b> and <b>Query</b> operations silently filter results via
/// <see cref="IResourceAccessEvaluator.FilterAsync{T}"/>, returning only resources the
/// caller is authorized to see.
/// </para>
/// <para>
/// <b>Create</b> checks the permission against a parent resource (or root defaults when
/// <c>parentResourceId</c> is <see langword="null"/>).
/// </para>
/// <para>
/// <b>Update</b> and <b>Delete</b> check the permission against the entity's own ACL.
/// </para>
/// <para>
/// Paging operations are intentionally excluded — post-query ACL filtering makes page
/// metadata (total count, has-next) unreliable. Use query overloads and page manually
/// when ACL filtering is required.
/// </para>
/// </remarks>
public interface IProtectedRepository<TEntity>
	: IRepository<TEntity>
	where TEntity : IEntity, IProtectedResource {

	/// <summary>
	/// Gets an entity by <paramref name="id"/> and verifies the caller has
	/// <paramref name="permission"/> on it.
	/// </summary>
	/// <exception cref="Exception">Thrown when the caller lacks the required permission.</exception>
	ValueTask<TEntity> GetAsync(
		string id,
		Permission permission,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets multiple entities by <paramref name="ids"/> and filters to only those
	/// the caller has <paramref name="permission"/> on.
	/// </summary>
	ValueTask<IReadOnlyList<TEntity>> GetManyAsync(
		IEnumerable<string> ids,
		Permission permission,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets all entities and filters to only those the caller has
	/// <paramref name="permission"/> on.
	/// </summary>
	ValueTask<IReadOnlyList<TEntity>> GetAllAsync(
		Permission permission,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Queries entities matching <paramref name="predicate"/> and filters to only those
	/// the caller has <paramref name="permission"/> on.
	/// </summary>
	ValueTask<IReadOnlyList<TEntity>> QueryAsync(
		Expression<Func<TEntity, bool>> predicate,
		Permission permission,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets the first entity matching <paramref name="predicate"/> that the caller has
	/// <paramref name="permission"/> on, or <see langword="null"/> if none qualifies.
	/// </summary>
	ValueTask<TEntity?> FirstOrNullAsync(
		Expression<Func<TEntity, bool>> predicate,
		Permission permission,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Creates an entity after verifying the caller has <paramref name="permission"/> on
	/// the parent resource identified by <paramref name="parentResourceId"/>.
	/// Pass <see langword="null"/> for <paramref name="parentResourceId"/> to check
	/// against root defaults.
	/// </summary>
	/// <exception cref="Exception">Thrown when the caller lacks the required permission.</exception>
	ValueTask<TEntity> CreateAsync(
		TEntity value,
		string? parentResourceId,
		Permission permission,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Updates an entity after verifying the caller has <paramref name="permission"/>
	/// on it.
	/// </summary>
	/// <exception cref="Exception">Thrown when the caller lacks the required permission.</exception>
	ValueTask<TEntity> UpdateAsync(
		TEntity value,
		Permission permission,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Deletes an entity after verifying the caller has <paramref name="permission"/>
	/// on it.
	/// </summary>
	/// <exception cref="Exception">Thrown when the caller lacks the required permission.</exception>
	ValueTask<TEntity> DeleteAsync(
		TEntity value,
		Permission permission,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Loads then deletes the entity identified by <paramref name="id"/> after verifying
	/// the caller has <paramref name="permission"/> on it.
	/// </summary>
	/// <exception cref="Exception">Thrown when the caller lacks the required permission.</exception>
	ValueTask<TEntity> DeleteAsync(
		string id,
		Permission permission,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Moves an entity to a new parent, updating its <see cref="IProtectedResource.ParentResourceId"/>
	/// and <see cref="IProtectedResource.AncestorResourceIds"/>, then cascading the ancestor chain
	/// update to all descendants.
	/// </summary>
	/// <param name="value">The entity to move.</param>
	/// <param name="newParentResourceId">
	/// The <see cref="IProtectedResource.ResourceId"/> of the new parent, or
	/// <see langword="null"/> to move to the root.
	/// </param>
	/// <param name="permission">The permission required on both the entity and the new parent.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <exception cref="Exception">Thrown when the caller lacks the required permission.</exception>
	/// <exception cref="InvalidOperationException">Thrown when the move would create a cycle.</exception>
	ValueTask MoveAsync(
		TEntity value,
		string? newParentResourceId,
		Permission permission,
		CancellationToken cancellationToken = default);

}
