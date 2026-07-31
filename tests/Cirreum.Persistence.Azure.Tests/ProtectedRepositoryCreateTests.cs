namespace Cirreum.Persistence.Azure.Tests;

using Cirreum.Authorization;
using Cirreum.Authorization.Resources;
using Cirreum.Exceptions;
using Cirreum.Persistence;
using Cirreum.Persistence.Internal;
using Microsoft.Extensions.Logging;

public class ProtectedRepositoryCreateTests {

	private static readonly Permission Write = new("tests", "write");

	private static DefaultProtectedRepository<TestFolder> CreateRepo(
		out IRepository<TestFolder> inner,
		out IResourceAccessEvaluator evaluator) {
		inner = Substitute.For<IRepository<TestFolder>>();
		evaluator = Substitute.For<IResourceAccessEvaluator>();
		return new DefaultProtectedRepository<TestFolder>(
			inner, evaluator, Microsoft.Extensions.Logging.Abstractions.NullLogger<DefaultProtectedRepository<TestFolder>>.Instance);
	}

	[Fact]
	public async Task Create_with_parent_mismatching_the_entity_declared_parent_throws_before_any_check() {

		var repo = CreateRepo(out var inner, out var evaluator);
		var entity = new TestFolder { ParentResourceId = "folder-A" };

		var act = () => repo.CreateAsync(entity, "folder-B", Write).AsTask();

		await act.Should().ThrowAsync<ArgumentException>()
			.WithMessage("*does not match the entity's declared*");
		await evaluator.DidNotReceive().CheckAsync<TestFolder>(
			Arg.Any<string?>(), Arg.Any<Permission>(), Arg.Any<CancellationToken>());
		await inner.DidNotReceive().CreateAsync(Arg.Any<TestFolder>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Create_denied_on_the_parent_throws_and_never_persists() {

		var repo = CreateRepo(out var inner, out var evaluator);
		var entity = new TestFolder { ParentResourceId = "folder-A" };
		evaluator.CheckAsync<TestFolder>("folder-A", Write, Arg.Any<CancellationToken>())
			.Returns(Result.Fail(new ForbiddenAccessException("denied")));

		var act = () => repo.CreateAsync(entity, "folder-A", Write).AsTask();

		await act.Should().ThrowAsync<ForbiddenAccessException>();
		await inner.DidNotReceive().CreateAsync(Arg.Any<TestFolder>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Create_populates_the_ancestor_chain_and_returns_the_refreshed_entity() {

		var repo = CreateRepo(out var inner, out var evaluator);
		var entity = new TestFolder { Id = "child-1", ParentResourceId = "parent-1" };
		var parent = new TestFolder { Id = "parent-1", AncestorResourceIds = ["root-1"] };
		var refreshed = new TestFolder { Id = "child-1", ParentResourceId = "parent-1", AncestorResourceIds = ["parent-1", "root-1"] };

		evaluator.CheckAsync<TestFolder>("parent-1", Write, Arg.Any<CancellationToken>())
			.Returns(Result.Success);
		inner.CreateAsync(entity, Arg.Any<CancellationToken>()).Returns(entity);
		inner.GetAsync("parent-1", Arg.Any<CancellationToken>()).Returns(parent);
		inner.GetAsync("child-1", Arg.Any<CancellationToken>()).Returns(refreshed);

		Action<IPatchOperationBuilder<TestFolder>>? capturedOps = null;
		await inner.UpdatePartialAsync(
			"child-1",
			Arg.Do<Action<IPatchOperationBuilder<TestFolder>>>(ops => capturedOps = ops),
			Arg.Any<string?>(),
			Arg.Any<CancellationToken>());

		var result = await repo.CreateAsync(entity, "parent-1", Write);

		result.Should().BeSameAs(refreshed);
		result.AncestorResourceIds.Should().Equal("parent-1", "root-1");

		capturedOps.Should().NotBeNull("the ancestor chain must be patched onto the created document");
		var builder = new PatchOperationBuilder<TestFolder>();
		capturedOps!(builder);
		builder.PatchOperations.Should().ContainSingle()
			.Which.Path.Should().Be("/ancestorResourceIds");
	}

	[Fact]
	public async Task Create_without_ancestor_support_returns_the_created_entity_unpatched() {

		var inner = Substitute.For<IRepository<TestNote>>();
		var evaluator = Substitute.For<IResourceAccessEvaluator>();
		var repo = new DefaultProtectedRepository<TestNote>(
			inner, evaluator, Microsoft.Extensions.Logging.Abstractions.NullLogger<DefaultProtectedRepository<TestNote>>.Instance);

		var entity = new TestNote { ParentResourceId = "parent-1" };
		evaluator.CheckAsync<TestNote>("parent-1", Write, Arg.Any<CancellationToken>())
			.Returns(Result.Success);
		inner.CreateAsync(entity, Arg.Any<CancellationToken>()).Returns(entity);

		var result = await repo.CreateAsync(entity, "parent-1", Write);

		result.Should().BeSameAs(entity);
		await inner.DidNotReceive().UpdatePartialAsync(
			Arg.Any<string>(),
			Arg.Any<Action<IPatchOperationBuilder<TestNote>>>(),
			Arg.Any<string?>(),
			Arg.Any<CancellationToken>());
	}

}
