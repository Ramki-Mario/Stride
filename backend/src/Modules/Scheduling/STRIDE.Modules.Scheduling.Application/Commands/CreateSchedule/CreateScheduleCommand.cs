using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Scheduling.Application.DTOs;

namespace STRIDE.Modules.Scheduling.Application.Commands.CreateSchedule;

public sealed record CreateScheduleCommand(
    string  Name,
    string? Description,
    Guid    WorkflowDefinitionId,
    string  CronExpression,
    bool    IsActive) : IRequest<Result<ScheduleDefinitionDto>>;
