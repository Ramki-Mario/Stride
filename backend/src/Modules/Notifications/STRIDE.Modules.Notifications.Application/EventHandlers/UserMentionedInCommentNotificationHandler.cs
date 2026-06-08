using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Notifications.Application.Commands.CreateNotification;
using STRIDE.Modules.Notifications.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Events;

namespace STRIDE.Modules.Notifications.Application.EventHandlers;

/// <summary>
/// Creates a <see cref="NotificationType.Mentioned"/> notification for each user
/// who is @mentioned in a newly posted comment.
/// </summary>
internal sealed class UserMentionedInCommentNotificationHandler
    : INotificationHandler<DomainEventNotification<UserMentionedInCommentEvent>>
{
    private readonly ISender _sender;
    private readonly ILogger<UserMentionedInCommentNotificationHandler> _logger;

    public UserMentionedInCommentNotificationHandler(
        ISender sender,
        ILogger<UserMentionedInCommentNotificationHandler> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task Handle(
        DomainEventNotification<UserMentionedInCommentEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        var workflowLabel = string.IsNullOrWhiteSpace(e.WorkflowName)
            ? "a workflow"
            : $"\"{e.WorkflowName}\"";

        var command = new CreateNotificationCommand(
            TenantId:    e.TenantId,
            RecipientId: e.MentionedUserId,
            Type:        NotificationType.Mentioned,
            Title:       $"You were mentioned in {workflowLabel}",
            Body:        $"Someone mentioned you in a comment on workflow {workflowLabel}. Open the workflow to view the discussion.",
            CreatedBy:   e.AuthorId);

        var result = await _sender.Send(command, cancellationToken);

        if (!result.IsSuccess)
            _logger.LogWarning(
                "Failed to create Mentioned notification for comment {CommentId}: {Error}",
                e.CommentId, result.Error);
    }
}
