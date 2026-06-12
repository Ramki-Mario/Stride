namespace STRIDE.Modules.Reporting.Application.ReadModels;

public sealed record TeamWorkloadItemDto(
    Guid                             UserId,
    string                           DisplayName,
    string                           Email,
    int                              ActiveStepCount,
    bool                             HasOverdueSteps,
    IReadOnlyList<TeamMemberStepDto> TopSteps);

public sealed record TeamMemberStepDto(
    Guid      StepId,
    string    StepName,
    Guid      WorkflowInstanceId,
    string    WorkflowName,
    DateTime? DueAt,
    bool      IsOverdue);
