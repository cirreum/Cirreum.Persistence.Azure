namespace Cirreum.Persistence;

using Cirreum.Authorization.Resources;
using System.Runtime.CompilerServices;
using Permission = Cirreum.Authorization.Permission;

/// <summary>
/// A repository interface for entities that carry embedded ACLs via <see cref="IProtectedResource"/>.
/// Exposes only permission-aware operations that automatically evaluate object-level access using
/// <see cref="IResourceAccessEvaluator"/>. The underlying <see cref="IRepository{TEntity}"/> is
/// reachable in a scoped, audited way via
/// <see cref="UseInnerRepositoryAsync(Func{IRepository{TEntity}, CancellationToken, ValueTask}, CancellationToken, string, string, int)"/>.
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
/// <para>
/// This interface intentionally does <b>not</b> extend <see cref="IRepository{TEntity}"/>.
/// Operations outside the ACL-aware surface (system maintenance, projections, cross-cutting
/// reads) must be performed via
/// <see cref="UseInnerRepositoryAsync(Func{IRepository{TEntity}, CancellationToken, ValueTask}, CancellationToken, string, string, int)"/>,
/// which logs every entry for audit.
/// </para>
/// </remarks>
public interface IProtectedRepository<TEntity>
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
	/// When <typeparamref name="TEntity"/> implements <see cref="IDeletableEntity"/>, Soft-Deletes the entity
	/// after verifying the caller has <paramref name="permission"/> on it.
	/// </summary>
	/// <param name="value">The entity to delete.</param>
	/// <param name="permission">The permission required on the entity.</param>
	/// <param name="cancellationToken">The cancellation token to use when making asynchronous operations.</param>
	/// <returns>A <see cref="ValueTask"/> representing the asynchronous delete operation.</returns>
	/// <exception cref="Exception">Thrown when the caller lacks the required permission.</exception>
	ValueTask DeleteAsync(
		TEntity value,
		Permission permission,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// When <typeparamref name="TEntity"/> implements <see cref="IDeletableEntity"/>, Soft-Deletes the entity
	/// identified by <paramref name="id"/> after verifying the caller has <paramref name="permission"/> on it.
	/// </summary>
	/// <param name="id">The string identifier.</param>
	/// <param name="permission">The permission required on the entity.</param>
	/// <param name="cancellationToken">The cancellation token to use when making asynchronous operations.</param>
	/// <returns>A <see cref="ValueTask"/> representing the asynchronous delete operation.</returns>
	/// <exception cref="Exception">Thrown when the caller lacks the required permission.</exception>
	ValueTask DeleteAsync(
		string id,
		Permission permission,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// When <paramref name="softDelete"/> is <see langword="true"/> and <typeparamref name="TEntity"/> implements
	/// <see cref="IDeletableEntity"/>, Soft-Deletes the entity after verifying the caller has
	/// <paramref name="permission"/> on it; otherwise performs a hard delete.
	/// </summary>
	/// <param name="value">The entity to delete.</param>
	/// <param name="permission">The permission required on the entity.</param>
	/// <param name="cancellationToken">The cancellation token to use when making asynchronous operations.</param>
	/// <param name="softDelete">When <see langword="true"/> and <typeparamref name="TEntity"/> implements <see cref="IDeletableEntity"/>, performs a soft delete instead of a hard delete.</param>
	/// <returns>A <see cref="ValueTask{TEntity}"/> representing the deleted entity.</returns>
	/// <exception cref="Exception">Thrown when the caller lacks the required permission.</exception>
	ValueTask<TEntity> DeleteAsync(
		TEntity value,
		Permission permission,
		CancellationToken cancellationToken,
		bool softDelete = true);

	/// <summary>
	/// When <paramref name="softDelete"/> is <see langword="true"/> and <typeparamref name="TEntity"/> implements
	/// <see cref="IDeletableEntity"/>, Soft-Deletes the entity identified by <paramref name="id"/> after verifying
	/// the caller has <paramref name="permission"/> on it; otherwise performs a hard delete.
	/// </summary>
	/// <param name="id">The string identifier.</param>
	/// <param name="permission">The permission required on the entity.</param>
	/// <param name="cancellationToken">The cancellation token to use when making asynchronous operations.</param>
	/// <param name="softDelete">When <see langword="true"/> and <typeparamref name="TEntity"/> implements <see cref="IDeletableEntity"/>, performs a soft delete instead of a hard delete.</param>
	/// <returns>A <see cref="ValueTask{TEntity}"/> representing the deleted entity.</returns>
	/// <exception cref="Exception">Thrown when the caller lacks the required permission.</exception>
	ValueTask<TEntity> DeleteAsync(
		string id,
		Permission permission,
		CancellationToken cancellationToken,
		bool softDelete = true);


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


	/// <summary>
	/// Provides scoped access to the underlying <see cref="IRepository{TEntity}"/> for
	/// operations that fall outside the ACL-aware surface (system maintenance, projections,
	/// cross-cutting reads, etc.). Each call is logged with the entity type for audit.
	/// </summary>
	/// <remarks>
	/// The inner repository reference is intentionally scoped to the lifetime of
	/// <paramref name="action"/> — do not capture or persist it outside the callback.
	/// </remarks>
	/// <param name="action">Asynchronous work to perform against the inner repository.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <param name="callerMember">Compiler-supplied; do not pass.</param>
	/// <param name="callerFile">Compiler-supplied; do not pass.</param>
	/// <param name="callerLine">Compiler-supplied; do not pass.</param>
	ValueTask UseInnerRepositoryAsync(
		Func<IRepository<TEntity>, CancellationToken, ValueTask> action,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string callerMember = "",
		[CallerFilePath] string callerFile = "",
		[CallerLineNumber] int callerLine = 0);

	/// <inheritdoc cref="UseInnerRepositoryAsync(Func{IRepository{TEntity}, CancellationToken, ValueTask}, CancellationToken, string, string, int)"/>
	ValueTask<TResult> UseInnerRepositoryAsync<TResult>(
		Func<IRepository<TEntity>, CancellationToken, ValueTask<TResult>> action,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string callerMember = "",
		[CallerFilePath] string callerFile = "",
		[CallerLineNumber] int callerLine = 0);

}
