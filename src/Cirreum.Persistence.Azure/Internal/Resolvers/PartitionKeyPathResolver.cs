namespace Cirreum.Persistence.Internal.Resolvers;

internal static class PartitionKeyPathResolver {

	public static string GetPartitionKeyPath<TEntity>() where TEntity : IEntity =>
		GetPartitionKeyPath(typeof(TEntity));

	public static string GetPartitionKeyPath(Type itemType) {

		var attributeType = typeof(PartitionKeyPathAttribute);

		return Attribute.GetCustomAttribute(
			itemType, attributeType) is PartitionKeyPathAttribute partitionKeyPathAttribute
			? partitionKeyPathAttribute.Path
			: "/id";

	}

}