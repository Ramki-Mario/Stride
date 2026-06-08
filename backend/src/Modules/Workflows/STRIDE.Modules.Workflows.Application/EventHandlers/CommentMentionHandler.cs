using System.Text.RegularExpressions;
using MediatR;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Events;

namespace STRIDE.Modules.Workflows.Application.EventHandlers;

/// <summary>
/// Parses @mentions from a newly posted comment and fires a
/// <see cref="UserMentionedInCommentEvent"/> for each mentioned user.
/// </summary>
/// <remarks>
/// Mention format: <c>@emailLocalPart</c> — the part of the email address before '@'.
/// Self-mentions (author == mentioned user) are silently dropped.
/// </remarks>
internal sealed class CommentMentionHandler
    : INotificationHandler<DomainEventNotification<CommentPostedEvent>>
{
    /// <summary>Matches one or more @handle tokens in the comment body.</summary>
    private static readonly Regex MentionRegex =
        new(@"@([\w.\-]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IPublisher         _publisher;
    private readonly IUserLookupService _userLookup;

    public CommentMentionHandler(IPublisher publisher, IUserLookupService userLookup)
    {
        _publisher   = publisher;
        _userLookup  = userLookup;
    }

    public async Task Handle(
        DomainEventNotification<CommentPostedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        // Extract unique lower-cased local parts from the comment body.
        var localParts = MentionRegex
            .Matches(e.Body)
            .Select(m => m.Groups[1].Value.ToLowerInvariant())
            .Distinct()
            .ToList();

        if (localParts.Count == 0)
            return;

        var userMap = await _userLookup.LookupByEmailLocalPartsAsync(
            e.TenantId, localParts, cancellationToken);

        foreach (var (_, mentionedUserId) in userMap)
        {
            // Skip self-mentions.
            if (mentionedUserId == e.AuthorId)
                continue;

            await _publisher.Publish(
                new DomainEventNotification<UserMentionedInCommentEvent>(
                    new UserMentionedInCommentEvent(
                        CommentId:         e.CommentId,
                        WorkflowInstanceId: e.WorkflowInstanceId,
                        WorkflowName:      e.WorkflowName,
                        TenantId:          e.TenantId,
                        AuthorId:          e.AuthorId,
                        MentionedUserId:   mentionedUserId)),
                cancellationToken);
        }
    }
}
