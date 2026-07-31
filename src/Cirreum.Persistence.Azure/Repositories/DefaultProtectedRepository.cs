namespace Cirreum.Persistence;

using Cirreum.Authorization.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

/// <summary>
/// Default implementation of <see cref="IProtectedRepository{TEntity}"/>. Composes an
/// <see cref="IRepository{TEntity}"/> for data access with an <see cref="IResourceAccessEvaluator"/>
/// for object-level ACL enforcement.
/// </summary>
/// <typeparam name="TEntity">
/// The entity type, which must implement both <see cref="IEntity"/> and <see cref="IProtectedResource"/>.
/// </typeparam>
/// <remarks>
/// <para>
/// All public members enforce permission checks via <see cref="IResourceAccessEvaluator"/>.
/// For operations outside the ACL-aware surface, callers must use
/// <see cref="UseInnerRepositoryAsync(Func{IRepository{TEntity}, CancellationToken, ValueTask}, CancellationToken, string, string, int)"/>,
/// which logs every entry for audit.
/// </para>
/// <para>
/// Registered as <b>Scoped</b> to match the lifetime of <see cref="IResourceAccessEvaluator"/>
/// and the inner repository.
/// </para>
/// </remarks>
sealed partial class DefaultProtectedRepository<TEntity>
	: IProtectedRepository<TEntity>
	where TEntity : IEntity, IProtectedResource {

	private static readonly ConcurrentDictionary<Type, bool> _ancestorSupportCache = new();

	private readonly IRepository<TEntity> _repository;
	private readonly IResourceAccessEvaluator _evaluator;
	private readonly ILogger<DefaultProtectedRepository<TEntity>> _logger;
	private readonly System.Text.Json.JsonNamingPolicy _namingPolicy;
	private string? _ancestorsJsonName;
	private string? _parentJsonName;

	/// <summary>
	/// The document property name for <see cref="IProtectedResource.AncestorResourceIds"/> under
	/// the instance's configured serializer — honors <c>[JsonPropertyName]</c>, else applies the
	/// configured naming policy. Keeps ancestor patches and the cascade query aligned with how the
	/// documents are actually serialized.
	/// </summary>
	private string AncestorsJsonName =>
		this._ancestorsJsonName ??= this.ResolveJsonName(nameof(IProtectedResource.AncestorResourceIds));

	/// <inheritdoc cref="AncestorsJsonName"/>
	private string ParentJsonName =>
		this._parentJsonName ??= this.ResolveJsonName(nameof(IProtectedResource.ParentResourceId));

	private string ResolveJsonName(string propertyName) {
		var property = typeof(TEntity).GetProperty(propertyName);
		var attribute = property?.GetCustomAttributes(typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute), true);
		if (attribute is [System.Text.Json.Serialization.JsonPropertyNameAttribute jsonAttribute, ..]) {
			return jsonAttribute.Name;
		}
		return this._namingPolicy.ConvertName(propertyName);
	}

	private static System.Text.Json.JsonNamingPolicy ResolveNamingPolicy(string serviceKey) =>
		Internal.InstanceSettingsRegistry.TryGetSettings(serviceKey)?.SerializationOptions.MappedNamingPolicy
			?? System.Text.Json.JsonNamingPolicy.CamelCase;

	/// <summary>
	/// Checks (cached per type) whether <typeparamref name="TEntity"/> has overridden
	/// <see cref="IProtectedResource.AncestorResourceIds"/> with a concrete property,
	/// indicating the entity supports materialized ancestor paths.
	/// </summary>
	private static bool SupportsAncestorPath() {
		return _ancestorSupportCache.GetOrAdd(typeof(TEntity), static type => {
			var map = type.GetInterfaceMap(typeof(IProtectedResource));
			var ifaceGetter = typeof(IProtectedResource)
				.GetProperty(nameof(IProtectedResource.AncestorResourceIds))!
				.GetGetMethod()!;

			var idx = Array.IndexOf(map.InterfaceMethods, ifaceGetter);
			if (idx < 0) {
				return false;
			}

			// If the target method is declared on the concrete type (not the interface), it's overridden
			return map.TargetMethods[idx].DeclaringType != typeof(IProtectedResource);
		});
	}

	/// <summary>
	/// Default (non-keyed) constructor.
	/// </summary>
	public DefaultProtectedRepository(
		IRepository<TEntity> repository,
		IResourceAccessEvaluator evaluator,
		ILogger<DefaultProtectedRepository<TEntity>> logger) {
		this._repository = repository;
		this._evaluator = evaluator;
		this._logger = logger;
		this._namingPolicy = ResolveNamingPolicy(Cirreum.ServiceProvider.Configuration.ServiceProviderSettings.DefaultKey);
	}

	/// <summary>
	/// Keyed constructor — resolves the inner <see cref="IRepository{TEntity}"/> using the
	/// same service key that this instance was registered with.
	/// </summary>
	public DefaultProtectedRepository(
		[ServiceKey] string key,
		IServiceProvider services,
		IResourceAccessEvaluator evaluator,
		ILogger<DefaultProtectedRepository<TEntity>> logger) {
		this._repository = services.GetRequiredKeyedService<IRepository<TEntity>>(key);
		this._evaluator = evaluator;
		this._logger = logger;
		this._namingPolicy = ResolveNamingPolicy(key);
	}

}
