using FluentAssertions;
using NSubstitute;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.Commands.CreateComment;
using STRIDE.Modules.Workflows.Application.Commands.DeleteComment;
using STRIDE.Modules.Workflows.Application.Commands.EditComment;
using STRIDE.Modules.Workflows.Application.Queries.ListWorkflowComments;
using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Application.Tests;

public sealed class CommentCommandHandlerTests
{
    private readonly IWorkflowCommentRepository  _comments   = Substitute.For<IWorkflowCommentRepository>();
    private readonly IWorkflowInstanceRepository _instances  = Substitute.For<IWorkflowInstanceRepository>();

    // ── CreateCommentCommandHandler ───────────────────────────────────────────

    [Fact]
    public async Task CreateComment_WhenInstanceNotFound_ReturnsFailure()
    {
        // Arrange
        _instances.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                  .Returns((WorkflowInstance?)null);

        var handler = new CreateCommentCommandHandler(_comments, _instances);
        var command = new CreateCommentCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Hello");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task CreateComment_WithEmptyBody_ReturnsFailure()
    {
        // Arrange
        var instance = BuildInstance();
        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>())
                  .Returns(instance);

        var handler = new CreateCommentCommandHandler(_comments, _instances);
        var command = new CreateCommentCommand(
            instance.TenantId, instance.Id, Guid.NewGuid(), "");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("empty");
    }

    [Fact]
    public async Task CreateComment_WithValidInput_ReturnsNewCommentId()
    {
        // Arrange
        var instance = BuildInstance();
        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>())
                  .Returns(instance);

        var handler = new CreateCommentCommandHandler(_comments, _instances);
        var command = new CreateCommentCommand(
            instance.TenantId, instance.Id, Guid.NewGuid(), "A valid comment.");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await _comments.Received(1).AddAsync(Arg.Any<WorkflowComment>(), Arg.Any<CancellationToken>());
        await _comments.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── EditCommentCommandHandler ─────────────────────────────────────────────

    [Fact]
    public async Task EditComment_WhenCommentNotFound_ReturnsFailure()
    {
        // Arrange
        _comments.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                 .Returns((WorkflowComment?)null);

        var handler = new EditCommentCommandHandler(_comments);
        var command = new EditCommentCommand(Guid.NewGuid(), Guid.NewGuid(), "New text");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task EditComment_ByNonAuthor_ReturnsFailure()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var comment  = WorkflowComment.Create(Guid.NewGuid(), Guid.NewGuid(), authorId, "Original");
        _comments.GetByIdAsync(comment.Id, Arg.Any<CancellationToken>())
                 .Returns(comment);

        var handler = new EditCommentCommandHandler(_comments);
        var command = new EditCommentCommand(comment.Id, Guid.NewGuid(), "Hacked");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("author");
    }

    [Fact]
    public async Task EditComment_ByAuthorWithValidBody_Succeeds()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var comment  = WorkflowComment.Create(Guid.NewGuid(), Guid.NewGuid(), authorId, "Original");
        _comments.GetByIdAsync(comment.Id, Arg.Any<CancellationToken>())
                 .Returns(comment);

        var handler = new EditCommentCommandHandler(_comments);
        var command = new EditCommentCommand(comment.Id, authorId, "Updated body");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        comment.Body.Should().Be("Updated body");
        await _comments.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── DeleteCommentCommandHandler ───────────────────────────────────────────

    [Fact]
    public async Task DeleteComment_WhenCommentNotFound_ReturnsFailure()
    {
        // Arrange
        _comments.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                 .Returns((WorkflowComment?)null);

        var handler = new DeleteCommentCommandHandler(_comments);
        var command = new DeleteCommentCommand(Guid.NewGuid(), Guid.NewGuid(), IsManager: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task DeleteComment_ByNonAuthorNonManager_ReturnsFailure()
    {
        // Arrange
        var comment = WorkflowComment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Body");
        _comments.GetByIdAsync(comment.Id, Arg.Any<CancellationToken>())
                 .Returns(comment);

        var handler = new DeleteCommentCommandHandler(_comments);
        var command = new DeleteCommentCommand(Guid.NewGuid(), comment.Id, IsManager: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("manager");
    }

    [Fact]
    public async Task DeleteComment_ByAuthor_SoftDeletesComment()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var comment  = WorkflowComment.Create(Guid.NewGuid(), Guid.NewGuid(), authorId, "Body");
        _comments.GetByIdAsync(comment.Id, Arg.Any<CancellationToken>())
                 .Returns(comment);

        var handler = new DeleteCommentCommandHandler(_comments);
        var command = new DeleteCommentCommand(authorId, comment.Id, IsManager: false);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        comment.IsDeleted.Should().BeTrue();
        comment.Body.Should().Be("(deleted)");
        await _comments.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteComment_ByManager_SoftDeletesOthersComment()
    {
        // Arrange
        var comment   = WorkflowComment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Body");
        var managerId = Guid.NewGuid();
        _comments.GetByIdAsync(comment.Id, Arg.Any<CancellationToken>())
                 .Returns(comment);

        var handler = new DeleteCommentCommandHandler(_comments);
        var command = new DeleteCommentCommand(managerId, comment.Id, IsManager: true);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        comment.IsDeleted.Should().BeTrue();
    }

    // ── ListWorkflowCommentsQueryHandler ──────────────────────────────────────

    [Fact]
    public async Task ListComments_ReturnsPagedResults()
    {
        // Arrange
        var instanceId = Guid.NewGuid();
        var tenantId   = Guid.NewGuid();

        var c1 = WorkflowComment.Create(instanceId, tenantId, Guid.NewGuid(), "First comment");
        var c2 = WorkflowComment.Create(instanceId, tenantId, Guid.NewGuid(), "Second comment");
        IReadOnlyList<WorkflowComment> items = new[] { c1, c2 };

        _comments.ListByInstanceAsync(instanceId, 1, 20, Arg.Any<CancellationToken>())
                 .Returns((items, 2));

        var handler = new ListWorkflowCommentsQueryHandler(_comments);
        var query   = new ListWorkflowCommentsQuery(instanceId, Page: 1, PageSize: 20);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task ListComments_ClampsPageBelowOneToOne()
    {
        // Arrange
        var instanceId = Guid.NewGuid();
        IReadOnlyList<WorkflowComment> items = Array.Empty<WorkflowComment>();
        _comments.ListByInstanceAsync(instanceId, 1, 20, Arg.Any<CancellationToken>())
                 .Returns((items, 0));

        var handler = new ListWorkflowCommentsQueryHandler(_comments);
        var query   = new ListWorkflowCommentsQuery(instanceId, Page: -5, PageSize: 20);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(1); // clamped from -5
        await _comments.Received(1).ListByInstanceAsync(instanceId, 1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListComments_ClampsPageSizeToHundred()
    {
        // Arrange
        var instanceId = Guid.NewGuid();
        IReadOnlyList<WorkflowComment> items = Array.Empty<WorkflowComment>();
        _comments.ListByInstanceAsync(instanceId, 1, 100, Arg.Any<CancellationToken>())
                 .Returns((items, 0));

        var handler = new ListWorkflowCommentsQueryHandler(_comments);
        var query   = new ListWorkflowCommentsQuery(instanceId, Page: 1, PageSize: 999);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PageSize.Should().Be(100); // clamped from 999
        await _comments.Received(1).ListByInstanceAsync(instanceId, 1, 100, Arg.Any<CancellationToken>());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static WorkflowInstance BuildInstance()
    {
        var def = WorkflowDefinition.Create(Guid.NewGuid(), "Test", null, Guid.NewGuid());
        def.AddStep("Step 1", null);
        def.Activate(Guid.NewGuid());
        def.ClearDomainEvents();
        var instance = WorkflowInstance.Start(def, Guid.NewGuid());
        instance.ClearDomainEvents();
        return instance;
    }
}
