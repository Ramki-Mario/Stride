using FluentAssertions;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Events;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Domain.Tests;

public sealed class WorkflowCommentTests
{
    // ── Create — validation ───────────────────────────────────────────────────

    [Fact]
    public void Create_WithEmptyBody_Throws()
    {
        var act = () => WorkflowComment.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "");

        act.Should().Throw<WorkflowDomainException>()
           .WithMessage("*empty*");
    }

    [Fact]
    public void Create_WithWhitespaceBody_Throws()
    {
        var act = () => WorkflowComment.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "   ");

        act.Should().Throw<WorkflowDomainException>()
           .WithMessage("*empty*");
    }

    [Fact]
    public void Create_WithBodyExceeding4000Chars_Throws()
    {
        var body = new string('x', 4001);
        var act  = () => WorkflowComment.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), body);

        act.Should().Throw<WorkflowDomainException>()
           .WithMessage("*4 000*");
    }

    // ── Create — success ──────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidBody_SetsProperties()
    {
        var instanceId = Guid.NewGuid();
        var tenantId   = Guid.NewGuid();
        var authorId   = Guid.NewGuid();
        const string body = "  Hello, world!  ";

        var comment = WorkflowComment.Create(instanceId, tenantId, authorId, body);

        comment.Id.Should().NotBeEmpty();
        comment.WorkflowInstanceId.Should().Be(instanceId);
        comment.TenantId.Should().Be(tenantId);
        comment.CreatedBy.Should().Be(authorId);
        comment.Body.Should().Be("Hello, world!"); // trimmed
        comment.IsDeleted.Should().BeFalse();
        comment.EditedAt.Should().BeNull();
    }

    [Fact]
    public void Create_RaisesCommentPostedEvent()
    {
        var instanceId = Guid.NewGuid();
        var tenantId   = Guid.NewGuid();
        var authorId   = Guid.NewGuid();
        const string body = "A valid comment.";

        var comment = WorkflowComment.Create(instanceId, tenantId, authorId, body);

        comment.DomainEvents.Should().ContainSingle(e => e is CommentPostedEvent);
        var ev = (CommentPostedEvent)comment.DomainEvents.Single(e => e is CommentPostedEvent);
        ev.CommentId.Should().Be(comment.Id);
        ev.WorkflowInstanceId.Should().Be(instanceId);
        ev.TenantId.Should().Be(tenantId);
        ev.AuthorId.Should().Be(authorId);
        ev.Body.Should().Be(body);
    }

    // ── Edit — validation ─────────────────────────────────────────────────────

    [Fact]
    public void Edit_ByNonAuthor_Throws()
    {
        var comment   = ValidComment();
        var strangerI = Guid.NewGuid();

        var act = () => comment.Edit("Updated body", strangerI);

        act.Should().Throw<WorkflowDomainException>()
           .WithMessage("*author*");
    }

    [Fact]
    public void Edit_WhenDeleted_Throws()
    {
        var authorId = Guid.NewGuid();
        var comment  = ValidComment(authorId);
        comment.SoftDelete(authorId, isManager: false);

        var act = () => comment.Edit("Updated body", authorId);

        act.Should().Throw<WorkflowDomainException>()
           .WithMessage("*deleted*");
    }

    [Fact]
    public void Edit_WithEmptyBody_Throws()
    {
        var authorId = Guid.NewGuid();
        var comment  = ValidComment(authorId);

        var act = () => comment.Edit("", authorId);

        act.Should().Throw<WorkflowDomainException>()
           .WithMessage("*empty*");
    }

    [Fact]
    public void Edit_WithBodyExceeding4000Chars_Throws()
    {
        var authorId = Guid.NewGuid();
        var comment  = ValidComment(authorId);

        var act = () => comment.Edit(new string('y', 4001), authorId);

        act.Should().Throw<WorkflowDomainException>()
           .WithMessage("*4 000*");
    }

    // ── Edit — success ────────────────────────────────────────────────────────

    [Fact]
    public void Edit_ByAuthor_UpdatesBodyAndSetsEditedAt()
    {
        var authorId = Guid.NewGuid();
        var comment  = ValidComment(authorId);

        comment.Edit("New content", authorId);

        comment.Body.Should().Be("New content");
        comment.EditedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    // ── SoftDelete — validation ───────────────────────────────────────────────

    [Fact]
    public void SoftDelete_ByNonAuthorNonManager_Throws()
    {
        var comment   = ValidComment();
        var strangerI = Guid.NewGuid();

        var act = () => comment.SoftDelete(strangerI, isManager: false);

        act.Should().Throw<WorkflowDomainException>()
           .WithMessage("*manager*");
    }

    // ── SoftDelete — success ──────────────────────────────────────────────────

    [Fact]
    public void SoftDelete_ByAuthor_ReplacesBodyAndSetsIsDeleted()
    {
        var authorId = Guid.NewGuid();
        var comment  = ValidComment(authorId);

        comment.SoftDelete(authorId, isManager: false);

        comment.IsDeleted.Should().BeTrue();
        comment.Body.Should().Be("(deleted)");
    }

    [Fact]
    public void SoftDelete_ByManager_SucceedsEvenIfNotAuthor()
    {
        var comment   = ValidComment();
        var managerId = Guid.NewGuid();

        comment.SoftDelete(managerId, isManager: true);

        comment.IsDeleted.Should().BeTrue();
        comment.Body.Should().Be("(deleted)");
    }

    [Fact]
    public void SoftDelete_CalledTwice_IsIdempotent()
    {
        var authorId = Guid.NewGuid();
        var comment  = ValidComment(authorId);

        comment.SoftDelete(authorId, isManager: false);
        var updatedAt = comment.UpdatedAt;

        // Second call should be a no-op — no exception, timestamps unchanged.
        var act = () => comment.SoftDelete(authorId, isManager: false);
        act.Should().NotThrow();
        comment.IsDeleted.Should().BeTrue();
        comment.UpdatedAt.Should().Be(updatedAt);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static WorkflowComment ValidComment(Guid? authorId = null)
        => WorkflowComment.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            authorId ?? Guid.NewGuid(),
            "A sample comment body.");
}
