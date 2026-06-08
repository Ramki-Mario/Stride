using System.Text.Json;
using MediatR;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Events;

namespace STRIDE.Modules.Workflows.Application.EventHandlers;

/// <summary>
/// Writes a <see cref="WorkflowActivityEvent"/> row for comment and attachment events.
/// </summary>
internal sealed class CommentPostedActivityHandler
    : INotificationHandler<DomainEventNotification<CommentPostedEvent>>
{
    private readonly IWorkflowActivityRepository _activity;

    public CommentPostedActivityHandler(IWorkflowActivityRepository activity)
        => _activity = activity;

    public async Task Handle(
        DomainEventNotification<CommentPostedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        var record = WorkflowActivityEvent.Create(
            e.WorkflowInstanceId,
            e.TenantId,
            ActivityEventType.CommentPosted,
            e.AuthorId);

        await _activity.AddAsync(record, cancellationToken);
        await _activity.SaveChangesAsync(cancellationToken);
    }
}

internal sealed class AttachmentUploadedActivityHandler
    : INotificationHandler<DomainEventNotification<AttachmentUploadedEvent>>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private readonly IWorkflowActivityRepository _activity;

    public AttachmentUploadedActivityHandler(IWorkflowActivityRepository activity)
        => _activity = activity;

    public async Task Handle(
        DomainEventNotification<AttachmentUploadedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        var payload = JsonSerializer.Serialize(
            new Dictionary<string, string?> { ["fileName"] = e.FileName },
            JsonOptions);

        var record = WorkflowActivityEvent.Create(
            e.WorkflowInstanceId,
            e.TenantId,
            ActivityEventType.AttachmentUploaded,
            e.UploadedBy,
            payload);

        await _activity.AddAsync(record, cancellationToken);
        await _activity.SaveChangesAsync(cancellationToken);
    }
}
