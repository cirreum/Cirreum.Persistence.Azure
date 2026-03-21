namespace Cirreum.Persistence.Internal.Resolvers;

using Microsoft.Azure.Cosmos;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text.Json.Serialization;

internal static class IndexingPolicyResolver {

	public static IndexingPolicy? GetIndexingPolicy<TEntity>() where TEntity : IEntity =>
		GetIndexingPolicy(typeof(TEntity));

	public static IndexingPolicy? GetIndexingPolicy(Type itemType) {

		var policyAttribute = itemType.GetCustomAttribute<IndexingPolicyAttribute>();

		if (policyAttribute is null) {
			return null;
		}

		var policy = new IndexingPolicy {
			IndexingMode = MapIndexingMode(policyAttribute.Mode),
			Automatic = policyAttribute.Automatic
		};

		ResolveExcludedPaths(itemType, policy);
		ResolveIncludedPaths(itemType, policy);
		ResolveCompositeIndexes(itemType, policy);
		ResolveSpatialIndexes(itemType, policy);

		return policy;
	}

	private static void ResolveExcludedPaths(Type itemType, IndexingPolicy policy) {

		foreach (var attribute in itemType.GetCustomAttributes<ExcludedPathAttribute>()) {
			policy.ExcludedPaths.Add(new ExcludedPath { Path = attribute.Path });
		}
	}

	private static void ResolveIncludedPaths(Type itemType, IndexingPolicy policy) {

		foreach (var property in itemType.GetProperties()) {

			var attribute = property.GetCustomAttribute<IncludedPathAttribute>();

			if (attribute is null) {
				continue;
			}

			var path = attribute.Path ?? $"/{ResolvePropertyName(property)}/?";
			policy.IncludedPaths.Add(new IncludedPath { Path = path });
		}
	}

	private static void ResolveCompositeIndexes(Type itemType, IndexingPolicy policy) {

		var groupMap = new Dictionary<string, List<(int Position, CompositePath Path)>>();

		foreach (var property in itemType.GetProperties()) {

			foreach (var attribute in property.GetCustomAttributes<CompositeIndexAttribute>()) {

				var path = attribute.Path ?? $"/{ResolvePropertyName(property)}";

				var compositePath = new CompositePath {
					Path = path,
					Order = MapCompositePathSortOrder(attribute.Order)
				};

				if (groupMap.TryGetValue(attribute.GroupName, out var paths)) {
					paths.Add((attribute.Position, compositePath));
				} else {
					groupMap[attribute.GroupName] = [(attribute.Position, compositePath)];
				}
			}
		}

		foreach (var group in groupMap.Values) {

			var compositeIndex = new Collection<CompositePath>();

			foreach (var entry in group.OrderBy(x => x.Position)) {
				compositeIndex.Add(entry.Path);
			}

			policy.CompositeIndexes.Add(compositeIndex);
		}
	}

	private static void ResolveSpatialIndexes(Type itemType, IndexingPolicy policy) {

		var spatialMap = new Dictionary<string, List<SpatialType>>();

		foreach (var property in itemType.GetProperties()) {

			foreach (var attribute in property.GetCustomAttributes<SpatialIndexAttribute>()) {

				var path = attribute.Path ?? $"/{ResolvePropertyName(property)}/*";

				if (spatialMap.TryGetValue(path, out var types)) {
					types.Add(MapSpatialType(attribute.SpatialType));
				} else {
					spatialMap[path] = [MapSpatialType(attribute.SpatialType)];
				}
			}
		}

		foreach (var (path, types) in spatialMap) {

			var spatialPath = new SpatialPath { Path = path };

			foreach (var type in types) {
				spatialPath.SpatialTypes.Add(type);
			}

			policy.SpatialIndexes.Add(spatialPath);
		}
	}

	private static string ResolvePropertyName(PropertyInfo property) {

		var jsonAttribute = property.GetCustomAttribute<JsonPropertyNameAttribute>();

		return jsonAttribute is not null
			? jsonAttribute.Name
			: property.Name.Camelize();
	}

	private static IndexingMode MapIndexingMode(Persistence.IndexingMode mode) =>
		mode switch {
			Persistence.IndexingMode.Consistent => IndexingMode.Consistent,
			Persistence.IndexingMode.Lazy => IndexingMode.Lazy,
			Persistence.IndexingMode.None => IndexingMode.None,
			_ => IndexingMode.Consistent
		};

	private static CompositePathSortOrder MapCompositePathSortOrder(Persistence.CompositePathSortOrder order) =>
		order switch {
			Persistence.CompositePathSortOrder.Ascending => CompositePathSortOrder.Ascending,
			Persistence.CompositePathSortOrder.Descending => CompositePathSortOrder.Descending,
			_ => CompositePathSortOrder.Ascending
		};

	private static SpatialType MapSpatialType(Persistence.SpatialType type) =>
		type switch {
			Persistence.SpatialType.Point => SpatialType.Point,
			Persistence.SpatialType.LineString => SpatialType.LineString,
			Persistence.SpatialType.Polygon => SpatialType.Polygon,
			Persistence.SpatialType.MultiPolygon => SpatialType.MultiPolygon,
			_ => SpatialType.Point
		};

}
