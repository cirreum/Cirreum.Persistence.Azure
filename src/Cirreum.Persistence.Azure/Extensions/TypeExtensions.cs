namespace Cirreum.Persistence.Extensions;

using System;

internal static class TypeExtensions {

	public static void EnsureIsItemType(this Type type) {
		if (!typeof(IEntity).IsAssignableFrom(type)) {
			throw new InvalidOperationException(
				$"The type {type.FullName} does not implement {typeof(IEntity).FullName}");
		}
	}

}