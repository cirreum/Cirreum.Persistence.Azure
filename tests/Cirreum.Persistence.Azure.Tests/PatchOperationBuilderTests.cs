namespace Cirreum.Persistence.Azure.Tests;

using Cirreum.Persistence.Internal;
using Microsoft.Azure.Cosmos;
using System.Text.Json;

public class PatchOperationBuilderTests {

	[Fact]
	public void Expression_paths_use_camelCase_by_default() {

		var builder = new PatchOperationBuilder<TestFolder>();
		builder.Set(x => x.InheritPermissions, false);

		builder.PatchOperations.Should().ContainSingle()
			.Which.Path.Should().Be("/inheritPermissions");
	}

	[Fact]
	public void Expression_paths_honor_the_configured_naming_policy() {

		var builder = new PatchOperationBuilder<TestFolder>(JsonNamingPolicy.SnakeCaseLower);
		builder.Set(x => x.InheritPermissions, false);

		builder.PatchOperations.Should().ContainSingle()
			.Which.Path.Should().Be("/inherit_permissions");
	}

	[Fact]
	public void Path_based_operations_are_recorded_for_in_memory_application() {

		var builder = new PatchOperationBuilder<TestFolder>();
		builder.SetByPath("ancestorResourceIds", (IReadOnlyList<string>)["p1", "r1"]);

		builder._rawPatchOperations.Should().ContainSingle();
		builder._rawPatchOperations[0].Path.Should().Be("ancestorResourceIds");
		builder._rawPatchOperations[0].Type.Should().Be(PatchOperationType.Set);
	}

}
