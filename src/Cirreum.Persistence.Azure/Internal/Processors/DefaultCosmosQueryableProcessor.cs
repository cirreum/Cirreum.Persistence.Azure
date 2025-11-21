namespace Cirreum.Persistence.Internal.Processors;

using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

class DefaultCosmosQueryableProcessor : ICosmosQueryableProcessor {

	public async ValueTask<(IReadOnlyList<TEntity> items, double charge)> IterateAsync<TEntity>(
		IQueryable<TEntity> queryable,
		CancellationToken cancellationToken = default)
		where TEntity : IEntity {

		var charge = 0.0;
		var results = new List<TEntity>();

		using (var iterator = queryable.ToFeedIterator()) {
			while (iterator.HasMoreResults) {

				var response = await iterator
					.ReadNextAsync(cancellationToken)
					.ConfigureAwait(false);

#if DEBUG
				if (response.Diagnostics.GetClientElapsedTime() > TimeSpan.FromMilliseconds(500)) {
					Console.WriteLine("************************************************************");
					Console.WriteLine(response.Diagnostics.ToString());
					Console.WriteLine("************************************************************");
				}
#endif

				charge += response.RequestCharge;

				results.AddRange(response.Resource);

			}
		}

		return (results.AsReadOnly(), charge);

	}

	public async ValueTask<(
		IReadOnlyList<TEntity> items,
		double charge, string? continuationToken)> IterateAsync<TEntity>(
		IQueryable<TEntity> queryable,
		int pageSize,
		CancellationToken cancellationToken = default)
		where TEntity : IEntity {

		var charge = 0.0;
		var results = new List<TEntity>();
		var resultSize = 0;
		string? continuationToken = null;

		using (var iterator = queryable.ToFeedIterator()) {
			while (resultSize < pageSize && iterator.HasMoreResults) {

				var feed = await iterator
					.ReadNextAsync(cancellationToken)
					.ConfigureAwait(false);

				foreach (var item in feed) {

					if (resultSize == pageSize) {
						break;
					}

					results.Add(item);
					resultSize++;

				}

				charge += feed.RequestCharge;
				continuationToken = feed.ContinuationToken;

			}
		}

		return (results.AsReadOnly(), charge, continuationToken);

	}

	public async ValueTask<(IReadOnlyList<TEntity> items, double charge)> IterateAsync<TEntity>(
		Container container,
		QueryDefinition queryDefinition,
		CancellationToken cancellationToken = default)
		where TEntity : IEntity {

		ArgumentNullException.ThrowIfNull(queryDefinition);

		var charge = 0.0;
		var results = new List<TEntity>();
		var options = new QueryRequestOptions {
			MaxConcurrency = -1
		};

		using (var queryIterator = container.GetItemQueryIterator<TEntity>(queryDefinition, requestOptions: options)) {
			while (queryIterator.HasMoreResults) {

				var response = await queryIterator
					.ReadNextAsync(cancellationToken)
					.ConfigureAwait(false);

				charge += response.RequestCharge;

				results.AddRange(response.Resource);

			}
		}

		return (results.AsReadOnly(), charge);

	}

	public async ValueTask<(IReadOnlyList<TEntity> items, double charge)> IterateGetAllAsync<TEntity>(
		Container container,
		CancellationToken cancellationToken = default)
		where TEntity : IEntity {

		var charge = 0.0;
		var results = new List<TEntity>();
		var options = new QueryRequestOptions {
			MaxConcurrency = -1
		};

		using (var queryIterator = container.GetItemQueryIterator<TEntity>(requestOptions: options)) {
			while (queryIterator.HasMoreResults) {

				var response = await queryIterator
					.ReadNextAsync(cancellationToken)
					.ConfigureAwait(false);

				charge += response.RequestCharge;

				results.AddRange(response.Resource);

			}
		}

		return (results.AsReadOnly(), charge);

	}

}