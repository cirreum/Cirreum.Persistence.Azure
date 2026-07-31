namespace Cirreum.Persistence.Internal;

using System.Reflection;

sealed class InternalPatchOperation {

	public InternalPatchOperation(PropertyInfo propertyInfo, object? newValue, PatchOperationType type) {
		this.PropertyInfo = propertyInfo;
		this.NewValue = newValue;
		this.Type = type;
	}

	public InternalPatchOperation(string path, object? newValue, PatchOperationType type) {
		this.Path = path;
		this.NewValue = newValue;
		this.Type = type;
	}

	public PatchOperationType Type { get; }

	/// <summary>Set for expression-based operations; null for path-based operations.</summary>
	public PropertyInfo? PropertyInfo { get; }

	/// <summary>Set for path-based operations (no leading slash); null for expression-based operations.</summary>
	public string? Path { get; }

	public object? NewValue { get; }
}
