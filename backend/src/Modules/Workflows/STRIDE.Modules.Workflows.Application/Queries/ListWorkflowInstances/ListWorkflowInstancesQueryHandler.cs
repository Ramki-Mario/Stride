using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.Helpers;
using STRIDE.Modules.Workflows.Domain.Enums;

namespace STRIDE.Modules.Workflows.Application.Queries.ListWorkflowInstances;

internal sealed class ListWorkflowInstancesQueryHandler
    : IRequestHandler<ListWorkflowInstancesQuery, Result<IReadOnlyList<WorkflowInstanceSummaryDto>>>
{
    private readonly IWorkflowInstanceRepository _instances;

    public ListWorkflowInstancesQueryHandler(IWorkflowInstanceRepository instances)
        => _instances = instances;

    public async Task<Result<IReadOnlyList<WorkflowInstanceSummaryDto>>> Handle(
        ListWorkflowInstancesQuery request,
        CancellationToken cancellationToken)
    {
        var instances = request.WorkflowDefinitionId.HasValue
            ? await _instances.GetByDefinitionIdAsync(request.WorkflowDefinitionId.Value, cancellationToken)
            : await _instances.GetAllAsync(cancellationToken);

        var summaries = instances
            .Where(i => !request.TeamId.HasValue || i.TeamId == request.TeamId)
            .OrderByDescending(i => i.UpdatedAt)
            .Select(i =>
            {
                var completedSteps = i.Steps.Count(s =>
                    s.Status is StepStatus.Completed or StepStatus.Skipped);

                return new WorkflowInstanceSummaryDto(
                    i.Id,
                    i.WorkflowDefinitionId,
                    i.WorkflowName,
                    i.Status,
                    i.Steps.Count,
                    completedSteps,
                    i.CreatedAt,
                    i.CompletedAt,
                    i.DeadlineAt,
                    SlaStatusComputer.Compute(i.CreatedAt, i.DeadlineAt, i.CompletedAt),
                    i.IsSlaBreached,
                    i.TeamId);
            })
            .ToList()
            .AsReadOnly();

        return Result.Success<IReadOnlyList<WorkflowInstanceSummaryDto>>(summaries);
    }
}
