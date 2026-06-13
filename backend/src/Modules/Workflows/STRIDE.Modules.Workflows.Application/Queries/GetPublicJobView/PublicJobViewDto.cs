using STRIDE.Modules.Workflows.Application.DTOs;

namespace STRIDE.Modules.Workflows.Application.Queries.GetPublicJobView;

/// <summary>
/// Redacted read model for the unauthenticated client-facing job view (US-178/US-179).
/// Intentionally omits user IDs, assignee names, field values, failure reasons, and
/// other data that should not leak to external parties.
/// </summary>
public sealed record PublicJobViewDto(
    string   WorkflowName,
    string   Status,
    DateTime StartedAt,
    DateTime? CompletedAt,
    DateTime? DeadlineAt,
    string?  SlaStatus,
    IReadOnlyList<PublicStepDto> Steps,
    DateTime? SignedOffAt,
    string?   SignedOffBy,
    PublicInvoiceDto? Invoice);

public sealed record PublicStepDto(
    string    StepName,
    int       Order,
    bool      IsRequired,
    string    Status,
    DateTime? CompletedAt,
    DateTime? DueAt);
