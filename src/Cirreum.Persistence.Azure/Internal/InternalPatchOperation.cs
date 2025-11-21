namespace Cirreum.Persistence.Internal;

using Microsoft.Azure.Cosmos;
using System.Reflection;

sealed class InternalPatchOperation(PropertyInfo propertyInfo, object? newValue, PatchOperationType type) {

	public PatchOperationType Type { get; } = type;

	public PropertyInfo PropertyInfo { get; } = propertyInfo;

	public object? NewValue { get; } = newValue;
}