using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Scheduling.Application.Abstractions;

namespace STRIDE.Modules.Scheduling.Application.Commands.DeleteSchedule;

internal sealed class DeleteScheduleCommandHandler : IRequestHandler<DeleteScheduleCommand, Result>
{
    private readonly IScheduleDefinitionRepository _repository;

    public DeleteScheduleCommandHandler(IScheduleDefinitionRepository repository)
        => _repository = repository;

    public async Task<Result> Handle(DeleteScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (schedule is null)
            return Result.Failure("Schedule not found.");

        schedule.SoftDelete();
        await _repository.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
