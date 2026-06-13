namespace STRIDE.Modules.Workflows.Application.Queries.GetPublicJobView;

/// <summary>
/// Redacted read model for the unauthenticated client-facing job view (US-178).
/// Intentionally omits user IDs, assignee names, billable amounts, field values,
/// failure reasons, and other data that should not leak to external parties.
/// </summary>
public sealed record PublicJobViewDto(
    string   WorkflowName,
    string   Status,
    DateTime StartedAt,
    DateTime? CompletedAt,
    DateTime? DeadlineAt,
    string?  SlaStatus,
    IReadOnlyList<PublicStepDto> Steps);

public sealed record PublicStepDto(
    string    StepName,
    int       Order,
    bool      IsRequired,
    string    Status,
    DateTime? CompletedAt,
    DateTime? DueAt);
