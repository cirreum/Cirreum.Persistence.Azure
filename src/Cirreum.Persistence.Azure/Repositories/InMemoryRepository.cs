namespace Cirreum.Persistence;

using Cirreum.Security;
using System;
using System.Net;
using System.Runtime.CompilerServices;

sealed partial class InMemoryRepository<TEntity>(
	IUserStateAccessor userAccessor,
	IDateTimeClock datetimeService)
	: IRepository<TEntity>
	where TEntity : IEntity {

	internal static long CurrentTs => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
	private readonly IUserStateAccessor _userAccessor = userAccessor;
	private readonly IDateTimeClock _datetimeService = datetimeService;

	private static CosmosException NotFound() => new CosmosException(string.Empty, HttpStatusCode.NotFound, 0, string.Empty, 0);
	private static CosmosException Conflict() => new CosmosException(string.Empty, HttpStatusCode.Conflict, 0, string.Empty, 0);
	private static CosmosException MismatchedEtags() => new CosmosException(string.Empty, HttpStatusCode.PreconditionFailed, 0, string.Empty, 0);

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

}