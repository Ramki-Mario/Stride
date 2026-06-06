using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.Abstractions;

namespace STRIDE.Modules.Workflows.Application.Queries.ListWorkflowDefinitions;

internal sealed class ListWorkflowDefinitionsQueryHandler
    : IRequestHandler<ListWorkflowDefinitionsQuery, Result<IReadOnlyList<WorkflowDefinitionSummaryDto>>>
{
    private readonly IWorkflowDefinitionRepository _definitions;

    public ListWorkflowDefinitionsQueryHandler(IWorkflowDefinitionRepository definitions)
        => _definitions = definitions;

    public async Task<Result<IReadOnlyList<WorkflowDefinitionSummaryDto>>> Handle(
        ListWorkflowDefinitionsQuery request,
        CancellationToken cancellationToken)
    {
        var definitions = await _definitions.GetAllAsync(cancellationToken);

        var summaries = definitions
            .OrderByDescending(d => d.UpdatedAt)
            .Select(d => new WorkflowDefinitionSummaryDto(
                d.Id,
                d.Name,
                d.Description,
                d.Status,
                d.Steps.Count,
                d.CreatedAt,
                d.UpdatedAt))
            .ToList()
            .AsReadOnly();

        return Result.Success<IReadOnlyList<WorkflowDefinitionSummaryDto>>(summaries);
    }
}
