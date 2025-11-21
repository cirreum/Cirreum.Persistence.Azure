namespace Cirreum.Persistence.Internal;

using Cirreum.Persistence.Extensions;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

sealed class PatchOperationBuilder<TEntity>
	: IPatchOperationBuilder<TEntity>
	where TEntity : IEntity {

	private readonly List<PatchOperation> _patchOperations = [];
	private readonly JsonNamingPolicy _namingStrategy = JsonNamingPolicy.CamelCase;
	private void CanAddPatchOperation() {
		if (this._patchOperations.Count > 10) {
			throw new InvalidOperationException("Cannot exceed 10 patch operations on a single entity.");
		}
	}

	internal readonly List<InternalPatchOperation> _rawPatchOperations = [];

	/// <inheritdoc/>
	public IReadOnlyList<PatchOperation> PatchOperations => this._patchOperations;

	/// <inheritdoc/>
	public IPatchOperationBuilder<TEntity> Add<TValue>(Expression<Func<TEntity, TValue>> expression, TValue value) {
		this.CanAddPatchOperation();
		var property = expression.GetPropertyInfo();
		var propertyName = this.GetPropertyName(property);
		this._rawPatchOperations.Add(new InternalPatchOperation(property, value, PatchOperationType.Add));
		this._patchOperations.Add(PatchOperation.Add($"/{propertyName}", value));
		return this;
	}

	/// <inheritdoc/>
	public IPatchOperationBuilder<TEntity> Set<TValue>(Expression<Func<TEntity, TValue>> expression, TValue value) {
		this.CanAddPatchOperation();
		var property = expression.GetPropertyInfo();
		var propertyName = this.GetPropertyName(property);
		this._rawPatchOperations.Add(new InternalPatchOperation(property, value, PatchOperationType.Set));
		this._patchOperations.Add(PatchOperation.Set($"/{propertyName}", value));
		return this;
	}

	/// <inheritdoc/>
	public IPatchOperationBuilder<TEntity> Replace<TValue>(Expression<Func<TEntity, TValue>> expression, TValue value) {
		this.CanAddPatchOperation();
		var property = expression.GetPropertyInfo();
		var propertyName = this.GetPropertyName(property);
		this._rawPatchOperations.Add(new InternalPatchOperation(property, value, PatchOperationType.Replace));
		this._patchOperations.Add(PatchOperation.Replace($"/{propertyName}", value));
		return this;
	}

	/// <inheritdoc/>
	public IPatchOperationBuilder<TEntity> Remove<TValue>(Expression<Func<TEntity, TValue>> expression) {
		this.CanAddPatchOperation();
		var property = expression.GetPropertyInfo();
		var propertyName = this.GetPropertyName(property);
		this._rawPatchOperations.Add(new InternalPatchOperation(property, null, PatchOperationType.Remove));
		this._patchOperations.Add(PatchOperation.Remove($"/{propertyName}"));
		return this;
	}

	/// <inheritdoc/>
	public IPatchOperationBuilder<TEntity> Increment<TValue>(Expression<Func<TEntity, TValue>> expression, long value) {
		this.CanAddPatchOperation();
		var property = expression.GetPropertyInfo();
		var propertyName = this.GetPropertyName(property);
		this._rawPatchOperations.Add(new InternalPatchOperation(property, value, PatchOperationType.Increment));
		this._patchOperations.Add(PatchOperation.Increment($"/{propertyName}", value));
		return this;
	}

	/// <inheritdoc/>
	public IPatchOperationBuilder<TEntity> Increment<TValue>(Expression<Func<TEntity, TValue>> expression, double value) {
		this.CanAddPatchOperation();
		var property = expression.GetPropertyInfo();
		var propertyName = this.GetPropertyName(property);
		this._rawPatchOperations.Add(new InternalPatchOperation(property, value, PatchOperationType.Increment));
		this._patchOperations.Add(PatchOperation.Increment($"/{propertyName}", value));
		return this;
	}

	private string GetPropertyName(PropertyInfo propertyInfo) {

		var attributes = propertyInfo.GetCustomAttributes(true);
		if (attributes.Length == 0) {
			return this._namingStrategy.ConvertName(propertyInfo.Name);
		}

		foreach (var attribute in attributes) {
			if (attribute is JsonPropertyNameAttribute jsonAttribute) {
				return jsonAttribute.Name;
			}
		}

		return this._namingStrategy.ConvertName(propertyInfo.Name);

	}


	/// <inheritdoc/>
	public IPatchOperationBuilder<TEntity> AddByPath<TValue>(string propertyPath, TValue value) {
		this.CanAddPatchOperation();
		// Ensure the path starts with a forward slash
		propertyPath = PatchOperationBuilder<TEntity>.EnsureLeadingSlash(propertyPath);
		this._patchOperations.Add(PatchOperation.Add(propertyPath, value));
		return this;
	}

	/// <inheritdoc/>
	public IPatchOperationBuilder<TEntity> SetByPath<TValue>(string propertyPath, TValue value) {
		this.CanAddPatchOperation();
		propertyPath = PatchOperationBuilder<TEntity>.EnsureLeadingSlash(propertyPath);
		this._patchOperations.Add(PatchOperation.Set(propertyPath, value));
		return this;
	}

	/// <inheritdoc/>
	public IPatchOperationBuilder<TEntity> ReplaceByPath<TValue>(string propertyPath, TValue value) {
		this.CanAddPatchOperation();
		propertyPath = PatchOperationBuilder<TEntity>.EnsureLeadingSlash(propertyPath);
		this._patchOperations.Add(PatchOperation.Replace(propertyPath, value));
		return this;
	}

	/// <inheritdoc/>
	public IPatchOperationBuilder<TEntity> RemoveByPath(string propertyPath) {
		this.CanAddPatchOperation();
		propertyPath = PatchOperationBuilder<TEntity>.EnsureLeadingSlash(propertyPath);
		this._patchOperations.Add(PatchOperation.Remove(propertyPath));
		return this;
	}

	/// <inheritdoc/>
	public IPatchOperationBuilder<TEntity> IncrementByPath(string propertyPath, long value) {
		this.CanAddPatchOperation();
		propertyPath = PatchOperationBuilder<TEntity>.EnsureLeadingSlash(propertyPath);
		this._patchOperations.Add(PatchOperation.Increment(propertyPath, value));
		return this;
	}

	/// <inheritdoc/>
	public IPatchOperationBuilder<TEntity> IncrementByPath(string propertyPath, double value) {
		this.CanAddPatchOperation();
		propertyPath = PatchOperationBuilder<TEntity>.EnsureLeadingSlash(propertyPath);
		this._patchOperations.Add(PatchOperation.Increment(propertyPath, value));
		return this;
	}

	private static string EnsureLeadingSlash(string propertyPath) {
		return propertyPath.StartsWith('/') ? propertyPath : $"/{propertyPath}";
	}

}