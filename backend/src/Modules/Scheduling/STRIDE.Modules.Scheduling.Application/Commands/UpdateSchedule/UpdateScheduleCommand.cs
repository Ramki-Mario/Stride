using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Scheduling.Application.DTOs;

namespace STRIDE.Modules.Scheduling.Application.Commands.UpdateSchedule;

public sealed record UpdateScheduleCommand(
    Guid    Id,
    string  Name,
    string? Description,
    Guid    WorkflowDefinitionId,
    string  CronExpression) : IRequest<Result<ScheduleDefinitionDto>>;
