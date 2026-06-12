namespace STRIDE.Modules.Reporting.Application.ReadModels;

public sealed record DashboardAlertSummaryDto(
    IReadOnlyList<OverdueAlertItemDto>        Overdue,
    IReadOnlyList<UnassignedStepAlertItemDto> UnassignedSteps,
    IReadOnlyList<SlaAtRiskAlertItemDto>      SlaAtRisk,
    IReadOnlyList<ReadyToInvoiceAlertItemDto> ReadyToInvoice);

public sealed record OverdueAlertItemDto(
    Guid    Id,
    string  WorkflowName,
    string? ClientName,
    int     MinutesOverdue);

public sealed record UnassignedStepAlertItemDto(
    Guid   StepId,
    string StepName,
    Guid   WorkflowInstanceId,
    string WorkflowName,
    int    MinutesWaiting);

public sealed record SlaAtRiskAlertItemDto(
    Guid    Id,
    string  WorkflowName,
    string? ClientName,
    int     MinutesRemaining);

public sealed record ReadyToInvoiceAlertItemDto(
    Guid     Id,
    string   WorkflowName,
    string?  ClientName,
    DateTime CompletedAt);
