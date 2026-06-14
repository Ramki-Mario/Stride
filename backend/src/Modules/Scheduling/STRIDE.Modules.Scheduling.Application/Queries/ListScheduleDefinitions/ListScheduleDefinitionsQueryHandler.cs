using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Scheduling.Application.Abstractions;
using STRIDE.Modules.Scheduling.Application.DTOs;

namespace STRIDE.Modules.Scheduling.Application.Queries.ListScheduleDefinitions;

internal sealed class ListScheduleDefinitionsQueryHandler
    : IRequestHandler<ListScheduleDefinitionsQuery, Result<IReadOnlyList<ScheduleDefinitionSummaryDto>>>
{
    private readonly IScheduleDefinitionRepository _repository;

    public ListScheduleDefinitionsQueryHandler(IScheduleDefinitionRepository repository)
        => _repository = repository;

    public async Task<Result<IReadOnlyList<ScheduleDefinitionSummaryDto>>> Handle(
        ListScheduleDefinitionsQuery query,
        CancellationToken            cancellationToken)
    {
        var schedules = await _repository.GetAllAsync(cancellationToken);

        var dtos = schedules
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new ScheduleDefinitionSummaryDto(
                Id:                   s.Id,
                Name:                 s.Name,
                WorkflowDefinitionId: s.WorkflowDefinitionId,
                CronExpression:       s.CronExpression.Value,
                IsActive:             s.IsActive,
                NextRunAt:            s.NextRunAt,
                CreatedAt:            s.CreatedAt))
            .ToList()
            .AsReadOnly();

        return Result.Success<IReadOnlyList<ScheduleDefinitionSummaryDto>>(dtos);
    }
}
