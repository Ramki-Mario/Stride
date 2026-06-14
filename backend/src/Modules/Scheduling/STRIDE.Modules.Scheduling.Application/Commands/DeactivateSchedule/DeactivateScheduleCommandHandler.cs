using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Scheduling.Application.Abstractions;

namespace STRIDE.Modules.Scheduling.Application.Commands.DeactivateSchedule;

internal sealed class DeactivateScheduleCommandHandler : IRequestHandler<DeactivateScheduleCommand, Result>
{
    private readonly IScheduleDefinitionRepository _repository;

    public DeactivateScheduleCommandHandler(IScheduleDefinitionRepository repository)
        => _repository = repository;

    public async Task<Result> Handle(DeactivateScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (schedule is null)
            return Result.Failure("Schedule not found.");

        schedule.Deactivate(request.DeactivatedBy);
        await _repository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
