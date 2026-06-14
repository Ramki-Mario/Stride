using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Scheduling.Application.Abstractions;
using STRIDE.Modules.Scheduling.Application.Commands.CreateSchedule;
using STRIDE.Modules.Scheduling.Application.DTOs;

namespace STRIDE.Modules.Scheduling.Application.Queries.GetScheduleDefinition;

internal sealed class GetScheduleDefinitionQueryHandler
    : IRequestHandler<GetScheduleDefinitionQuery, Result<ScheduleDefinitionDto>>
{
    private readonly IScheduleDefinitionRepository _repository;

    public GetScheduleDefinitionQueryHandler(IScheduleDefinitionRepository repository)
        => _repository = repository;

    public async Task<Result<ScheduleDefinitionDto>> Handle(
        GetScheduleDefinitionQuery query,
        CancellationToken          cancellationToken)
    {
        var schedule = await _repository.GetByIdAsync(query.Id, cancellationToken);
        if (schedule is null)
            return Result.Failure<ScheduleDefinitionDto>("Schedule not found.");

        return Result.Success(CreateScheduleCommandHandler.ToDto(schedule));
    }
}
