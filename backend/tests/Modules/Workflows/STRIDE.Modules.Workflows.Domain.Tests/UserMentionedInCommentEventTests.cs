using FluentAssertions;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Events;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Domain.Tests;

/// <summary>
/// Domain-layer tests for @mention-related events and the WorkflowComment entity
/// changes introduced in US-167.
/// </summary>
public sealed class UserMentionedInCommentEventTests
{
    private static readonly Guid CommentId    = Guid.NewGuid();
    private static readonly Guid InstanceId   = Guid.NewGuid();
    private static readonly Guid TenantId     = Guid.NewGuid();
    private static readonly Guid AuthorId     = Guid.NewGuid();
    private static readonly Guid MentionedId  = Guid.NewGuid();

    // ── UserMentionedInCommentEvent record tests ───────────────────────────

    [Fact]
    public void UserMentionedInCommentEvent_StoresAllFields()
    {
        var evt = new UserMentionedInCommentEvent(
            CommentId:          CommentId,
            WorkflowInstanceId: InstanceId,
            WorkflowName:       "Onboarding Flow",
            TenantId:           TenantId,
            AuthorId:           AuthorId,
            MentionedUserId:    MentionedId);

        evt.CommentId.Should().Be(CommentId);
        evt.WorkflowInstanceId.Should().Be(InstanceId);
        evt.WorkflowName.Should().Be("Onboarding Flow");
        evt.TenantId.Should().Be(TenantId);
        evt.AuthorId.Should().Be(AuthorId);
        evt.MentionedUserId.Should().Be(MentionedId);
    }

    [Fact]
    public void UserMentionedInCommentEvent_TwoInstancesWithSameData_AreEqual()
    {
        var a = new UserMentionedInCommentEvent(
            CommentId, InstanceId, "Flow A", TenantId, AuthorId, MentionedId);
        var b = new UserMentionedInCommentEvent(
            CommentId, InstanceId, "Flow A", TenantId, AuthorId, MentionedId);

        a.Should().Be(b);
    }

    // ── WorkflowComment.Create — WorkflowName forwarding tests ────────────

    [Fact]
    public void Create_WithWorkflowName_RaisesCommentPostedEventWithName()
    {
        var comment = WorkflowComment.Create(
            workflowInstanceId: InstanceId,
            tenantId:           TenantId,
            authorId:           AuthorId,
            body:               "Hello @alice.smith please review",
            workflowName:       "Annual Audit");

        var raised = comment.DomainEvents
            .OfType<CommentPostedEvent>()
            .SingleOrDefault();

        raised.Should().NotBeNull();
        raised!.WorkflowName.Should().Be("Annual Audit");
        raised.Body.Should().Be("Hello @alice.smith please review");
    }

    [Fact]
    public void Create_WithoutWorkflowName_RaisesCommentPostedEventWithEmptyName()
    {
        var comment = WorkflowComment.Create(
            workflowInstanceId: InstanceId,
            tenantId:           TenantId,
            authorId:           AuthorId,
            body:               "No mention here");

        var raised = comment.DomainEvents
            .OfType<CommentPostedEvent>()
            .Single();

        raised.WorkflowName.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithEmptyBody_ThrowsWorkflowDomainException()
    {
        var act = () => WorkflowComment.Create(InstanceId, TenantId, AuthorId, "   ");
        act.Should().Throw<WorkflowDomainException>();
    }

    // ── CommentPostedEvent record tests ────────────────────────────────────

    [Fact]
    public void CommentPostedEvent_WorkflowName_DefaultsToEmpty()
    {
        var evt = new CommentPostedEvent(
            CommentId, InstanceId, TenantId, AuthorId, "body");

        evt.WorkflowName.Should().BeEmpty();
    }

    [Fact]
    public void CommentPostedEvent_ExplicitWorkflowName_IsPreserved()
    {
        var evt = new CommentPostedEvent(
            CommentId, InstanceId, TenantId, AuthorId, "body", "My Workflow");

        evt.WorkflowName.Should().Be("My Workflow");
    }
}
