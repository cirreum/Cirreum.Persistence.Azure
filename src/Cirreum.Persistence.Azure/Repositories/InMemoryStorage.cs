namespace Cirreum.Persistence;

using System.Collections.Concurrent;

internal static class InMemoryStorage {

	private static ConcurrentDictionary<Type, ConcurrentDictionary<string, string>?>
		TypeToConcurrentDictionary { get; } = [];

	internal static IEnumerable<string> GetValues<TEntity>()
		where TEntity : IEntity {

		if (TypeToConcurrentDictionary.TryGetValue(typeof(TEntity), out var value)) {
			return value?.Values ?? [];
		}

		TypeToConcurrentDictionary[typeof(TEntity)] = new ConcurrentDictionary<string, string>();
		return TypeToConcurrentDictionary[typeof(TEntity)]?.Values ?? [];
	}

	internal static ConcurrentDictionary<string, string> GetDictionary<TEntity>()
		where TEntity : IEntity {

		if (TypeToConcurrentDictionary.TryGetValue(typeof(TEntity), out var value)) {
			return value ?? new ConcurrentDictionary<string, string>();
		}

		TypeToConcurrentDictionary[typeof(TEntity)] = new ConcurrentDictionary<string, string>();
		return TypeToConcurrentDictionary[typeof(TEntity)] ?? new ConcurrentDictionary<string, string>();
	}

}