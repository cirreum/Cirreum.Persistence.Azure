namespace Cirreum.Persistence;

using Cirreum.Persistence.Internal.Logging;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

/// <summary>
/// Scoped access to the inner <see cref="IRepository{TEntity}"/> for operations that
/// fall outside the ACL-aware surface. Every entry is logged for audit.
/// </summary>
sealed partial class DefaultProtectedRepository<TEntity> {

	/// <inheritdoc/>
	public ValueTask UseInnerRepositoryAsync(
		Func<IRepository<TEntity>, CancellationToken, ValueTask> action,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string callerMember = "",
		[CallerFilePath] string callerFile = "",
		[CallerLineNumber] int callerLine = 0) {

		ArgumentNullException.ThrowIfNull(action);
		this._logger.LogInnerRepositoryScopeOpened<TEntity>(callerMember, callerFile, callerLine);
		return action(this._repository, cancellationToken);
	}

	/// <inheritdoc/>
	public ValueTask<TResult> UseInnerRepositoryAsync<TResult>(
		Func<IRepository<TEntity>, CancellationToken, ValueTask<TResult>> action,
		CancellationToken cancellationToken = default,
		[CallerMemberName] string callerMember = "",
		[CallerFilePath] string callerFile = "",
		[CallerLineNumber] int callerLine = 0) {

		ArgumentNullException.ThrowIfNull(action);
		this._logger.LogInnerRepositoryScopeOpened<TEntity>(callerMember, callerFile, callerLine);
		return action(this._repository, cancellationToken);
	}

}
