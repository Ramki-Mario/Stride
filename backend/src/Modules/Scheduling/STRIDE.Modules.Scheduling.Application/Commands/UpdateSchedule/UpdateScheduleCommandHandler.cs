using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Scheduling.Application.Abstractions;
using STRIDE.Modules.Scheduling.Application.Commands.CreateSchedule;
using STRIDE.Modules.Scheduling.Application.DTOs;
using STRIDE.Modules.Scheduling.Domain.ValueObjects;

namespace STRIDE.Modules.Scheduling.Application.Commands.UpdateSchedule;

internal sealed class UpdateScheduleCommandHandler
    : IRequestHandler<UpdateScheduleCommand, Result<ScheduleDefinitionDto>>
{
    private readonly IScheduleDefinitionRepository _repository;

    public UpdateScheduleCommandHandler(IScheduleDefinitionRepository repository)
        => _repository = repository;

    public async Task<Result<ScheduleDefinitionDto>> Handle(
        UpdateScheduleCommand request,
        CancellationToken     cancellationToken)
    {
        var schedule = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (schedule is null)
            return Result.Failure<ScheduleDefinitionDto>("Schedule not found.");

        var cronVo    = CronExpression.Create(request.CronExpression);
        var nextRunAt = schedule.IsActive
            ? CreateScheduleCommandHandler.ComputeNextRun(request.CronExpression)
            : (DateTime?)null;

        schedule.Update(
            name:                 request.Name,
            description:          request.Description,
            workflowDefinitionId: request.WorkflowDefinitionId,
            cronExpression:       cronVo,
            nextRunAt:            nextRunAt,
            updatedBy:            Guid.Empty);

        await _repository.SaveChangesAsync(cancellationToken);
        return Result.Success(CreateScheduleCommandHandler.ToDto(schedule));
    }
}
