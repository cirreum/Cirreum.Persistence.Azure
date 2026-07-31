namespace Cirreum.Persistence.Azure.Tests;

using Cirreum.Authorization.Resources;
using Cirreum.Persistence;

// Folder-like entity with a concrete AncestorResourceIds property — SupportsAncestorPath() true.
public sealed record TestFolder : IEntity, IProtectedResource {
	public string Id { get; set; } = Guid.NewGuid().ToString();
	public string EntityType { get; init; } = nameof(TestFolder);
	public string PartitionKey => this.Id;
	public string? ResourceId => this.Id;
	public string? ParentResourceId { get; set; }
	public IReadOnlyList<AccessEntry> AccessList { get; set; } = [];
	public bool InheritPermissions { get; set; } = true;
	public IReadOnlyList<string> AncestorResourceIds { get; set; } = [];
}

// Entity WITHOUT a concrete AncestorResourceIds override — SupportsAncestorPath() false.
public sealed record TestNote : IEntity, IProtectedResource {
	public string Id { get; set; } = Guid.NewGuid().ToString();
	public string EntityType { get; init; } = nameof(TestNote);
	public string PartitionKey => this.Id;
	public string? ResourceId => this.Id;
	public string? ParentResourceId { get; set; }
	public IReadOnlyList<AccessEntry> AccessList { get; set; } = [];
	public bool InheritPermissions { get; set; } = true;
}
