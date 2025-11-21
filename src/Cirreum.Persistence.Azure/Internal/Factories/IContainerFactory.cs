namespace Cirreum.Persistence.Internal.Factories;

using Microsoft.Azure.Cosmos;
using System.Threading.Tasks;

internal interface IContainerFactory<TEntity>
	where TEntity : IEntity {
	Task<Container> GetContainerAsync(string key);
}