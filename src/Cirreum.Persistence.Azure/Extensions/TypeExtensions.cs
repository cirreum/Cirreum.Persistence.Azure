namespace Cirreum.Persistence.Extensions;

using System;
using System.Collections.Generic;

internal static class TypeExtensions {

	public static void IsItem(this Type type) {
		if (!typeof(IEntity).IsAssignableFrom(type)) {
			throw new InvalidOperationException(
				$"The type {type.FullName} does not implement {typeof(IEntity).FullName}");
		}
	}

	public static void AreAllItems(this IReadOnlyList<Type> types) {
		foreach (var type in types) {
			type.IsItem();
		}
	}

}