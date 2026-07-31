namespace Cirreum.Persistence.Azure.Tests;

using Cirreum.Persistence;
using Cirreum.Security;

public class InMemoryRepositoryPatchTests {

	private static InMemoryRepository<TestFolder> CreateRepo() {
		var userAccessor = Substitute.For<IUserStateAccessor>();
		var clock = Substitute.For<IDateTimeClock>();
		return new InMemoryRepository<TestFolder>(userAccessor, clock);
	}

	[Fact]
	public async Task SetByPath_is_applied_to_the_stored_entity() {

		var repo = CreateRepo();
		var entity = new TestFolder { Id = Guid.NewGuid().ToString(), ParentResourceId = "parent-1" };
		await repo.CreateAsync(entity);

		await repo.UpdatePartialAsync(
			entity.Id,
			ops => ops.SetByPath("ancestorResourceIds", (IReadOnlyList<string>)["parent-1", "root-1"]));

		var read = await repo.GetAsync(entity.Id);
		read.AncestorResourceIds.Should().Equal("parent-1", "root-1");
	}

	[Fact]
	public async Task SetByPath_resolves_the_property_through_camelCase_naming() {

		var repo = CreateRepo();
		var entity = new TestFolder { Id = Guid.NewGuid().ToString(), InheritPermissions = true };
		await repo.CreateAsync(entity);

		await repo.UpdatePartialAsync(
			entity.Id,
			ops => ops.SetByPath("inheritPermissions", false));

		var read = await repo.GetAsync(entity.Id);
		read.InheritPermissions.Should().BeFalse();
	}

}
