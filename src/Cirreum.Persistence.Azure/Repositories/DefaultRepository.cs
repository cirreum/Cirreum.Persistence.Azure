namespace Cirreum.Persistence;

using Cirreum.Persistence.Configuration;
using Cirreum.Persistence.Extensions;
using Cirreum.Security;
using Cirreum.ServiceProvider.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Runtime.CompilerServices;

sealed partial class DefaultRepository<TEntity>
	: IRepository<TEntity>
	where TEntity : IEntity {

	private readonly ILogger<DefaultRepository<TEntity>> _logger;
	private readonly IUserStateAccessor _userAccessor;
	private readonly IDateTimeClock _datetimeService;
	private readonly ICosmosQueryableProcessor _queryableProcessor;
	private readonly IContainerFactory<TEntity> _containerProvider;
	private readonly AzureCosmosInstanceSettings _settings;
	private readonly string _serviceKey;

	(bool OptimizeBandwidth, ItemRequestOptions Options) RequestOptions =>
		(this._settings.OptimizeBandwidth,
		new ItemRequestOptions {
			EnableContentResponseOnWrite = !this._settings.OptimizeBandwidth
		});

	/// <summary>
	/// Keyed Constructor.
	/// </summary>
	/// <param name="key">The service key this instance was resolved from.</param>
	/// <param name="containerProvider"></param>
	/// <param name="logger"></param>
	/// <param name="queryableProcessor"></param>
	/// <param name="userAccessor"></param>
	/// <param name="datetimeService"></param>
	public DefaultRepository(
		[ServiceKey] string key,
		IContainerFactory<TEntity> containerProvider,
		ICosmosQueryableProcessor queryableProcessor,
		ILogger<DefaultRepository<TEntity>> logger,
		IUserStateAccessor userAccessor,
		IDateTimeClock datetimeService) =>
		(this._serviceKey, this._settings, this._containerProvider, this._logger, this._queryableProcessor, this._userAccessor, this._datetimeService) =
		(key, InstanceSettingsRegistry.GetSettings(key), containerProvider, logger, queryableProcessor, userAccessor, datetimeService);

	/// <summary>
	/// Default Constructor.
	/// </summary>
	/// <param name="containerProvider"></param>
	/// <param name="logger"></param>
	/// <param name="queryableProcessor"></param>
	/// <param name="userAccessor"></param>
	/// <param name="datetimeService"></param>
	public DefaultRepository(
		IContainerFactory<TEntity> containerProvider,
		ICosmosQueryableProcessor queryableProcessor,
		ILogger<DefaultRepository<TEntity>> logger,
		IUserStateAccessor userAccessor,
		IDateTimeClock datetimeService) =>
		(this._serviceKey, this._settings, this._containerProvider, this._logger, this._queryableProcessor, this._userAccessor, this._datetimeService) =
		(ServiceProviderSettings.DefaultKey, InstanceSettingsRegistry.GetSettings(ServiceProviderSettings.DefaultKey), containerProvider, logger, queryableProcessor, userAccessor, datetimeService);

	const char IdPKSeparator = '\u03B6';
	private static string GenerateItemId(IEntity entity) {
		if (entity.PartitionKey == entity.Id) {
			return entity.Id;
		}
		return string.Concat(entity.Id, IdPKSeparator, entity.PartitionKey);
	}
	private static PartitionKey ResolvePartitionKey(string entityId) {
		var separatorIndex = entityId.IndexOf(IdPKSeparator);
		if (separatorIndex == -1) {
			return new PartitionKey(entityId);
		}

		var key = entityId.AsSpan()[(separatorIndex + 1)..];
		return new PartitionKey(key.ToString());
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static PartitionKey ValidatePartitionKey(TEntity entity) {
		var pk = new PartitionKey(entity.PartitionKey);
		var idPk = ResolvePartitionKey(entity.Id);
		if (idPk != pk) {
			throw new InvalidOperationException("The partition key value has been modified.");
		}
		return pk;
	}

	private static Expression<Func<TEntity, bool>> CombineWithDeleteFilter(
		Expression<Func<TEntity, bool>>? predicate,
		bool includeDeleted) {

		if (!typeof(IDeletableEntity).IsAssignableFrom(typeof(TEntity)) || includeDeleted) {
			return predicate ?? (x => true);
		}

		var deleteFilter = (Expression<Func<TEntity, bool>>)(x =>
			!((IDeletableEntity)x).IsDeleted);

		return predicate == null
			? deleteFilter
			: predicate.AndAlso(deleteFilter);
	}

}