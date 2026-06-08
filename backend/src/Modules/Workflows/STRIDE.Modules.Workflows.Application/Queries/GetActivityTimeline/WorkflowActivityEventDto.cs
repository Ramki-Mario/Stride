namespace STRIDE.Modules.Workflows.Application.Queries.GetActivityTimeline;

/// <summary>
/// Read-facing DTO for a single activity event on the workflow timeline.
/// </summary>
public sealed record WorkflowActivityEventDto(
    Guid                        Id,
    int                         EventType,
    string                      EventTypeLabel,
    Guid                        ActorUserId,
    string                      Description,
    IReadOnlyDictionary<string, string?> Payload,
    DateTime                    OccurredAt);

/// <summary>
/// Paginated wrapper returned by <see cref="GetActivityTimelineQuery"/>.
/// </summary>
public sealed record PagedActivityDto(
    IReadOnlyList<WorkflowActivityEventDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
