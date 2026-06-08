using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Notifications.Application.Commands.CreateNotification;
using STRIDE.Modules.Notifications.Application.EventHandlers;
using STRIDE.Modules.Notifications.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Events;

namespace STRIDE.Modules.Notifications.Application.Tests;

/// <summary>
/// Unit tests for <see cref="UserMentionedInCommentNotificationHandler"/> (US-167).
/// </summary>
public sealed class UserMentionedNotificationHandlerTests
{
    private static readonly Guid CommentId   = Guid.NewGuid();
    private static readonly Guid InstanceId  = Guid.NewGuid();
    private static readonly Guid TenantId    = Guid.NewGuid();
    private static readonly Guid AuthorId    = Guid.NewGuid();
    private static readonly Guid MentionedId = Guid.NewGuid();

    private static DomainEventNotification<UserMentionedInCommentEvent> MakeNotification(
        string workflowName = "My Workflow") =>
        new(new UserMentionedInCommentEvent(
            CommentId, InstanceId, workflowName, TenantId, AuthorId, MentionedId));

    private static (ISender sender, UserMentionedInCommentNotificationHandler handler) BuildSut()
    {
        var sender  = Substitute.For<ISender>();
        var logger  = NullLogger<UserMentionedInCommentNotificationHandler>.Instance;
        var handler = new UserMentionedInCommentNotificationHandler(sender, logger);

        sender.Send(Arg.Any<CreateNotificationCommand>(), Arg.Any<CancellationToken>())
              .Returns(Result.Success(new CreateNotificationResult(Guid.NewGuid())));

        return (sender, handler);
    }

    [Fact]
    public async Task Handle_SendsNotificationToMentionedUser()
    {
        var (sender, handler) = BuildSut();

        await handler.Handle(MakeNotification(), CancellationToken.None);

        await sender.Received(1).Send(
            Arg.Is<CreateNotificationCommand>(c =>
                c.RecipientId == MentionedId &&
                c.TenantId    == TenantId    &&
                c.CreatedBy   == AuthorId    &&
                c.Type        == NotificationType.Mentioned),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TitleContainsWorkflowName()
    {
        var (sender, handler) = BuildSut();

        await handler.Handle(MakeNotification("Annual Compliance"), CancellationToken.None);

        await sender.Received(1).Send(
            Arg.Is<CreateNotificationCommand>(c => c.Title.Contains("Annual Compliance")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyWorkflowName_TitleContainsGenericFallback()
    {
        var (sender, handler) = BuildSut();

        await handler.Handle(MakeNotification(""), CancellationToken.None);

        await sender.Received(1).Send(
            Arg.Is<CreateNotificationCommand>(c => c.Title.Contains("a workflow")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SendFailure_DoesNotThrow()
    {
        var sender  = Substitute.For<ISender>();
        var logger  = NullLogger<UserMentionedInCommentNotificationHandler>.Instance;
        var handler = new UserMentionedInCommentNotificationHandler(sender, logger);

        sender.Send(Arg.Any<CreateNotificationCommand>(), Arg.Any<CancellationToken>())
              .Returns(Result.Failure<CreateNotificationResult>("DB error"));

        // Should not throw — failures are logged, not propagated.
        var act = async () => await handler.Handle(MakeNotification(), CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
