namespace Cirreum.Persistence.Internal.Resolvers;

using Microsoft.Azure.Cosmos;
using System.Reflection;

internal static class UniqueKeyPolicyResolver {

	static readonly Type ukAttributeType = typeof(UniqueKeyAttribute);

	public static UniqueKeyPolicy? GetUniqueKeyPolicy<TEntity>() where TEntity : IEntity =>
		GetUniqueKeyPolicy(typeof(TEntity));

	public static UniqueKeyPolicy? GetUniqueKeyPolicy(Type itemType) {

		var keyNameToPathsMap = new Dictionary<string, List<string>>();

		foreach ((var uniqueKey, var propertyName) in itemType.GetProperties()
					 .Where(x => Attribute.IsDefined(x, ukAttributeType))
					 .Select(x => (x.GetCustomAttribute<UniqueKeyAttribute>(), x.Name))) {

			if (uniqueKey is null) {
				continue;
			}

			var propertyValue = (uniqueKey.PropertyPath ?? $"/{propertyName ?? ""}")!;


			if (keyNameToPathsMap.TryGetValue(uniqueKey.KeyName, out var value)
				&& value is not null) {
				value.Add(propertyValue);
				continue;
			}

			keyNameToPathsMap[uniqueKey.KeyName] = [propertyValue];

		}

		if (keyNameToPathsMap.Count == 0) {
			return null;
		}

		var policy = new UniqueKeyPolicy();

		foreach (var keyNameToPaths in keyNameToPathsMap) {

			var key = new UniqueKey();

			foreach (var path in keyNameToPaths.Value) {
				key.Paths.Add(path);
			}

			policy.UniqueKeys.Add(key);

		}

		return policy;

	}

}