using FluentAssertions;
using MediatR;
using NSubstitute;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.EventHandlers;
using STRIDE.Modules.Workflows.Domain.Events;

namespace STRIDE.Modules.Workflows.Application.Tests;

/// <summary>
/// Unit tests for <see cref="CommentMentionHandler"/> (US-167).
/// </summary>
public sealed class CommentMentionHandlerTests
{
    private readonly IPublisher         _publisher   = Substitute.For<IPublisher>();
    private readonly IUserLookupService _userLookup  = Substitute.For<IUserLookupService>();

    private readonly Guid _commentId    = Guid.NewGuid();
    private readonly Guid _instanceId   = Guid.NewGuid();
    private readonly Guid _tenantId     = Guid.NewGuid();
    private readonly Guid _authorId     = Guid.NewGuid();
    private readonly Guid _aliceId      = Guid.NewGuid();
    private readonly Guid _bobId        = Guid.NewGuid();

    private CommentMentionHandler BuildHandler() =>
        new(_publisher, _userLookup);

    private DomainEventNotification<CommentPostedEvent> Notification(string body, string workflowName = "My WF") =>
        new(new CommentPostedEvent(
            _commentId, _instanceId, _tenantId, _authorId, body, workflowName));

    // ── No mentions ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoMentions_DoesNotLookUpUsersOrPublish()
    {
        var handler = BuildHandler();

        await handler.Handle(Notification("Plain text without any at-sign"), CancellationToken.None);

        await _userLookup.DidNotReceiveWithAnyArgs()
            .LookupByEmailLocalPartsAsync(default, default!, default);
        await _publisher.DidNotReceiveWithAnyArgs()
            .Publish(Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    // ── Single mention ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_OneMention_LooksUpCorrectLocalPart()
    {
        _userLookup.LookupByEmailLocalPartsAsync(_tenantId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, Guid> { ["alice"] = _aliceId });

        var handler = BuildHandler();
        await handler.Handle(Notification("Hey @alice please check this"), CancellationToken.None);

        await _userLookup.Received(1).LookupByEmailLocalPartsAsync(
            _tenantId,
            Arg.Is<IEnumerable<string>>(parts => parts.Contains("alice")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OneMention_PublishesUserMentionedEvent()
    {
        _userLookup.LookupByEmailLocalPartsAsync(_tenantId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, Guid> { ["alice"] = _aliceId });

        var handler = BuildHandler();
        await handler.Handle(Notification("Hey @alice please check this", "Audit Flow"), CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<DomainEventNotification<UserMentionedInCommentEvent>>(n =>
                n.DomainEvent.MentionedUserId    == _aliceId     &&
                n.DomainEvent.AuthorId           == _authorId    &&
                n.DomainEvent.CommentId          == _commentId   &&
                n.DomainEvent.WorkflowInstanceId == _instanceId  &&
                n.DomainEvent.WorkflowName       == "Audit Flow" &&
                n.DomainEvent.TenantId           == _tenantId),
            Arg.Any<CancellationToken>());
    }

    // ── Multiple distinct mentions ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_TwoDistinctMentions_PublishesTwoEvents()
    {
        _userLookup.LookupByEmailLocalPartsAsync(_tenantId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, Guid>
            {
                ["alice"] = _aliceId,
                ["bob"]   = _bobId,
            });

        var handler = BuildHandler();
        await handler.Handle(Notification("@alice and @bob please review"), CancellationToken.None);

        await _publisher.Received(2).Publish(
            Arg.Any<DomainEventNotification<UserMentionedInCommentEvent>>(),
            Arg.Any<CancellationToken>());
    }

    // ── Duplicate mention (same handle twice) ──────────────────────────────────

    [Fact]
    public async Task Handle_DuplicateMention_PublishesOnlyOnce()
    {
        _userLookup.LookupByEmailLocalPartsAsync(_tenantId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, Guid> { ["alice"] = _aliceId });

        var handler = BuildHandler();
        await handler.Handle(Notification("@alice see @alice above"), CancellationToken.None);

        // Lookup receives deduplicated local parts (only "alice" once).
        await _userLookup.Received(1).LookupByEmailLocalPartsAsync(
            _tenantId,
            Arg.Is<IEnumerable<string>>(parts => parts.Count() == 1),
            Arg.Any<CancellationToken>());

        // Only one notification published.
        await _publisher.Received(1).Publish(
            Arg.Any<DomainEventNotification<UserMentionedInCommentEvent>>(),
            Arg.Any<CancellationToken>());
    }

    // ── Self-mention ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_SelfMention_DoesNotPublishEvent()
    {
        // Alice IS the author — self-mentions should be silently skipped.
        _userLookup.LookupByEmailLocalPartsAsync(_tenantId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, Guid> { ["alice"] = _authorId });

        var notification = new DomainEventNotification<CommentPostedEvent>(
            new CommentPostedEvent(_commentId, _instanceId, _tenantId, _authorId, "@alice is me"));

        var handler = BuildHandler();
        await handler.Handle(notification, CancellationToken.None);

        await _publisher.DidNotReceiveWithAnyArgs()
            .Publish(Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    // ── Unknown @handle (no user found) ───────────────────────────────────────

    [Fact]
    public async Task Handle_UnknownHandle_DoesNotPublishEvent()
    {
        _userLookup.LookupByEmailLocalPartsAsync(_tenantId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, Guid>()); // empty — no match

        var handler = BuildHandler();
        await handler.Handle(Notification("Hey @nobody please check"), CancellationToken.None);

        await _publisher.DidNotReceiveWithAnyArgs()
            .Publish(Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    // ── Mention carries correct workflow name ──────────────────────────────────

    [Fact]
    public async Task Handle_MentionedEvent_ContainsWorkflowName()
    {
        _userLookup.LookupByEmailLocalPartsAsync(_tenantId, Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, Guid> { ["alice"] = _aliceId });

        var handler = BuildHandler();
        await handler.Handle(Notification("@alice FYI", "Annual Compliance Workflow"), CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<DomainEventNotification<UserMentionedInCommentEvent>>(n =>
                n.DomainEvent.WorkflowName == "Annual Compliance Workflow"),
            Arg.Any<CancellationToken>());
    }
}
