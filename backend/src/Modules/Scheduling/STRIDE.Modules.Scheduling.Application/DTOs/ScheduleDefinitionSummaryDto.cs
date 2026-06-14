namespace STRIDE.Modules.Scheduling.Application.DTOs;

public sealed record ScheduleDefinitionSummaryDto(
    Guid     Id,
    string   Name,
    Guid     WorkflowDefinitionId,
    string   CronExpression,
    bool     IsActive,
    DateTime? NextRunAt,
    DateTime CreatedAt);
