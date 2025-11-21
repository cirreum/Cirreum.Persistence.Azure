namespace Cirreum.Persistence.Internal.Resolvers;

internal static class ContainerNameResolver {

	public static string GetContainerName<TEntity>() where TEntity : IEntity =>
		GetContainerName(typeof(TEntity));

	public static string GetContainerName(Type itemType) {

		var attributeType = typeof(ContainerAttribute);

		var attribute = Attribute.GetCustomAttribute(itemType, attributeType);

		return attribute is ContainerAttribute containerAttribute ?
			containerAttribute.Name :
			itemType.Name;

	}

}