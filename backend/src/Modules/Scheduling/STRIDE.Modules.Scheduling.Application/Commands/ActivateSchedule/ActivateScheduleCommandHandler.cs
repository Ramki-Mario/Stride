using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Scheduling.Application.Abstractions;
using STRIDE.Modules.Scheduling.Application.Commands.CreateSchedule;

namespace STRIDE.Modules.Scheduling.Application.Commands.ActivateSchedule;

internal sealed class ActivateScheduleCommandHandler : IRequestHandler<ActivateScheduleCommand, Result>
{
    private readonly IScheduleDefinitionRepository _repository;

    public ActivateScheduleCommandHandler(IScheduleDefinitionRepository repository)
        => _repository = repository;

    public async Task<Result> Handle(ActivateScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (schedule is null)
            return Result.Failure("Schedule not found.");

        var nextRunAt = CreateScheduleCommandHandler.ComputeNextRun(schedule.CronExpression.Value);
        schedule.Activate(nextRunAt, request.ActivatedBy);
        await _repository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
