namespace STRIDE.Modules.Scheduling.Application.DTOs;

public sealed record ScheduleDefinitionDto(
    Guid     Id,
    string   Name,
    string?  Description,
    Guid     WorkflowDefinitionId,
    string   CronExpression,
    bool     IsActive,
    DateTime? NextRunAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);
