namespace Cirreum.Persistence.Azure.Tests;

using Cirreum.Authorization;
using Cirreum.Authorization.Resources;
using Cirreum.Exceptions;
using Cirreum.Persistence;
using Cirreum.Persistence.Internal;
using Microsoft.Extensions.Logging;

public class ProtectedRepositoryMoveTests {

	private static readonly Permission Move = new("tests", "move");

	private static DefaultProtectedRepository<TestFolder> CreateRepo(
		out IRepository<TestFolder> inner,
		out IResourceAccessEvaluator evaluator) {
		inner = Substitute.For<IRepository<TestFolder>>();
		evaluator = Substitute.For<IResourceAccessEvaluator>();
		return new DefaultProtectedRepository<TestFolder>(
			inner, evaluator, Microsoft.Extensions.Logging.Abstractions.NullLogger<DefaultProtectedRepository<TestFolder>>.Instance);
	}

	[Fact]
	public async Task Move_that_would_create_a_cycle_throws() {

		var repo = CreateRepo(out var inner, out var evaluator);
		var entity = new TestFolder { Id = "folder-X" };
		var newParent = new TestFolder { Id = "folder-child", AncestorResourceIds = ["folder-X"] };

		evaluator.CheckAsync(entity, Move, Arg.Any<CancellationToken>()).Returns(Result.Success);
		evaluator.CheckAsync<TestFolder>("folder-child", Move, Arg.Any<CancellationToken>())
			.Returns(Result.Success);
		inner.GetAsync("folder-child", Arg.Any<CancellationToken>()).Returns(newParent);

		var act = () => repo.MoveAsync(entity, "folder-child", Move).AsTask();

		await act.Should().ThrowAsync<InvalidOperationException>()
			.WithMessage("*would create a cycle*");
		await inner.DidNotReceive().UpdatePartialAsync(
			Arg.Any<string>(),
			Arg.Any<Action<IPatchOperationBuilder<TestFolder>>>(),
			Arg.Any<string?>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Move_denied_on_the_entity_throws_before_any_write() {

		var repo = CreateRepo(out var inner, out var evaluator);
		var entity = new TestFolder { Id = "folder-X" };
		evaluator.CheckAsync(entity, Move, Arg.Any<CancellationToken>())
			.Returns(Result.Fail(new ForbiddenAccessException("denied")));

		var act = () => repo.MoveAsync(entity, "folder-Y", Move).AsTask();

		await act.Should().ThrowAsync<ForbiddenAccessException>();
		await inner.DidNotReceive().UpdatePartialAsync(
			Arg.Any<string>(),
			Arg.Any<Action<IPatchOperationBuilder<TestFolder>>>(),
			Arg.Any<string?>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Move_patches_parent_and_ancestor_chain_with_serializer_aligned_paths() {

		var repo = CreateRepo(out var inner, out var evaluator);
		var entity = new TestFolder { Id = "folder-X" };
		var newParent = new TestFolder { Id = "folder-Y", AncestorResourceIds = ["root-1"] };

		evaluator.CheckAsync(entity, Move, Arg.Any<CancellationToken>()).Returns(Result.Success);
		evaluator.CheckAsync<TestFolder>("folder-Y", Move, Arg.Any<CancellationToken>())
			.Returns(Result.Success);
		inner.GetAsync("folder-Y", Arg.Any<CancellationToken>()).Returns(newParent);
		inner.QueryAsync(
				Arg.Any<string>(),
				Arg.Any<IEnumerable<KeyValuePair<string, string>>>(),
				Arg.Any<CancellationToken>())
			.Returns([]);

		Action<IPatchOperationBuilder<TestFolder>>? capturedOps = null;
		await inner.UpdatePartialAsync(
			"folder-X",
			Arg.Do<Action<IPatchOperationBuilder<TestFolder>>>(ops => capturedOps = ops),
			Arg.Any<string?>(),
			Arg.Any<CancellationToken>());

		await repo.MoveAsync(entity, "folder-Y", Move);

		capturedOps.Should().NotBeNull();
		var builder = new PatchOperationBuilder<TestFolder>();
		capturedOps!(builder);
		builder.PatchOperations.Should().HaveCount(2);
		builder.PatchOperations[0].Path.Should().Be("/parentResourceId");
		builder.PatchOperations[1].Path.Should().Be("/ancestorResourceIds");
	}

}
