using System.Text.Json;
using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;

namespace STRIDE.Modules.Workflows.Application.Queries.GetActivityTimeline;

internal sealed class GetActivityTimelineQueryHandler
    : IRequestHandler<GetActivityTimelineQuery, Result<PagedActivityDto>>
{
    private readonly IWorkflowActivityRepository _activity;
    private readonly IWorkflowInstanceRepository _instances;

    public GetActivityTimelineQueryHandler(
        IWorkflowActivityRepository activity,
        IWorkflowInstanceRepository instances)
    {
        _activity  = activity;
        _instances = instances;
    }

    public async Task<Result<PagedActivityDto>> Handle(
        GetActivityTimelineQuery request,
        CancellationToken cancellationToken)
    {
        // Verify the instance exists and belongs to this tenant.
        var instance = await _instances.GetByIdAsync(request.WorkflowInstanceId, cancellationToken);
        if (instance is null)
            return Result.Failure<PagedActivityDto>("Workflow instance not found.");

        var page     = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var (items, totalCount) = await _activity.ListByInstanceAsync(
            request.WorkflowInstanceId, page, pageSize, request.Ascending, cancellationToken);

        var dtos = items
            .Select(MapToDto)
            .ToList()
            .AsReadOnly();

        return Result.Success(new PagedActivityDto(dtos, totalCount, page, pageSize));
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static WorkflowActivityEventDto MapToDto(WorkflowActivityEvent e)
    {
        var payload = ParsePayload(e.Payload);
        return new WorkflowActivityEventDto(
            e.Id,
            (int)e.EventType,
            GetLabel(e.EventType),
            e.ActorUserId,
            BuildDescription(e.EventType, payload),
            payload,
            e.OccurredAt);
    }

    private static IReadOnlyDictionary<string, string?> ParsePayload(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new Dictionary<string, string?>();

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string?>>(json)
                   ?? new Dictionary<string, string?>();
        }
        catch
        {
            return new Dictionary<string, string?>();
        }
    }

    private static string GetLabel(ActivityEventType type) => type switch
    {
        ActivityEventType.WorkflowStarted     => "Workflow started",
        ActivityEventType.WorkflowCompleted   => "Workflow completed",
        ActivityEventType.WorkflowCancelled   => "Workflow cancelled",
        ActivityEventType.WorkflowSlaBreached => "SLA breached",
        ActivityEventType.WorkflowPaused      => "Workflow paused",
        ActivityEventType.WorkflowResumed     => "Workflow resumed",
        ActivityEventType.StepAssigned        => "Step assigned",
        ActivityEventType.StepCompleted       => "Step completed",
        ActivityEventType.StepFailed          => "Step failed",
        ActivityEventType.StepSkipped         => "Step skipped",
        ActivityEventType.StepOverdue         => "Step overdue",
        ActivityEventType.CommentPosted       => "Comment posted",
        ActivityEventType.AttachmentUploaded  => "File uploaded",
        _                                     => type.ToString(),
    };

    private static string BuildDescription(
        ActivityEventType type,
        IReadOnlyDictionary<string, string?> p)
    {
        var stepName = p.TryGetValue("stepName", out var sn) ? sn : null;
        var reason   = p.TryGetValue("reason",   out var r)  ? r  : null;
        var fileName = p.TryGetValue("fileName", out var fn) ? fn : null;

        return type switch
        {
            ActivityEventType.WorkflowStarted     => "Workflow was started.",
            ActivityEventType.WorkflowCompleted   => "Workflow was completed.",
            ActivityEventType.WorkflowCancelled   => "Workflow was cancelled.",
            ActivityEventType.WorkflowSlaBreached => "SLA deadline has been breached.",
            ActivityEventType.WorkflowPaused      => "Workflow was paused.",
            ActivityEventType.WorkflowResumed     => "Workflow was resumed.",
            ActivityEventType.StepAssigned        => stepName is not null
                ? $"Step \"{stepName}\" was assigned."
                : "A step was assigned.",
            ActivityEventType.StepCompleted       => stepName is not null
                ? $"Step \"{stepName}\" was completed."
                : "A step was completed.",
            ActivityEventType.StepFailed          => stepName is not null && reason is not null
                ? $"Step \"{stepName}\" failed: {reason}"
                : stepName is not null
                    ? $"Step \"{stepName}\" failed."
                    : "A step failed.",
            ActivityEventType.StepSkipped         => stepName is not null
                ? $"Step \"{stepName}\" was skipped."
                : "A step was skipped.",
            ActivityEventType.StepOverdue         => stepName is not null
                ? $"Step \"{stepName}\" is overdue."
                : "A step is overdue.",
            ActivityEventType.CommentPosted       => "A comment was posted.",
            ActivityEventType.AttachmentUploaded  => fileName is not null
                ? $"File \"{fileName}\" was uploaded."
                : "A file was uploaded.",
            _ => GetLabel(type),
        };
    }
}
