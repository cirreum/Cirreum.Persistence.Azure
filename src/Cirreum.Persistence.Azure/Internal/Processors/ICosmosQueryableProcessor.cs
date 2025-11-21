namespace Cirreum.Persistence.Internal.Processors;

using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

interface ICosmosQueryableProcessor {

	ValueTask<(IReadOnlyList<TEntity> items, double charge)> IterateAsync<TEntity>(
		IQueryable<TEntity> queryable,
		CancellationToken cancellationToken = default)
		where TEntity : IEntity;

	ValueTask<(IReadOnlyList<TEntity> items, double charge, string? continuationToken)> IterateAsync<TEntity>(
		IQueryable<TEntity> queryable,
		int pageSize,
		CancellationToken cancellationToken = default)
		where TEntity : IEntity;

	ValueTask<(IReadOnlyList<TEntity> items, double charge)> IterateAsync<TEntity>(
		Container container,
		QueryDefinition queryDefinition,
		CancellationToken cancellationToken = default)
		where TEntity : IEntity;

	ValueTask<(IReadOnlyList<TEntity> items, double charge)> IterateGetAllAsync<TEntity>(
		Container container,
		CancellationToken cancellationToken = default)
		where TEntity : IEntity;

}