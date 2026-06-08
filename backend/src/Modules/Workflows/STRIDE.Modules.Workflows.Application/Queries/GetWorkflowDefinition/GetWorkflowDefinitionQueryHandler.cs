using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.Abstractions;

namespace STRIDE.Modules.Workflows.Application.Queries.GetWorkflowDefinition;

internal sealed class GetWorkflowDefinitionQueryHandler
    : IRequestHandler<GetWorkflowDefinitionQuery, Result<WorkflowDefinitionDto>>
{
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly ILogger<GetWorkflowDefinitionQueryHandler> _logger;

    public GetWorkflowDefinitionQueryHandler(
        IWorkflowDefinitionRepository definitions,
        ILogger<GetWorkflowDefinitionQueryHandler> logger)
    {
        _definitions = definitions;
        _logger      = logger;
    }

    public async Task<Result<WorkflowDefinitionDto>> Handle(
        GetWorkflowDefinitionQuery request,
        CancellationToken cancellationToken)
    {
        var definition = await _definitions.GetByIdAsync(request.WorkflowDefinitionId, cancellationToken);
        if (definition is null)
        {
            _logger.LogWarning(
                "GetWorkflowDefinition: definition {Id} not found", request.WorkflowDefinitionId);
            return Result.Failure<WorkflowDefinitionDto>(
                $"Workflow definition '{request.WorkflowDefinitionId}' not found.");
        }

        var dto = new WorkflowDefinitionDto(
            definition.Id,
            definition.TenantId,
            definition.Name,
            definition.Description,
            definition.Status,
            definition.CreatedAt,
            definition.UpdatedAt,
            definition.CreatedBy,
            definition.Steps
                .OrderBy(s => s.Order)
                .Select(s => new StepDefinitionDto(s.Id, s.Name, s.Description, s.Order, s.IsRequired, s.RequiredRoleId))
                .ToList()
                .AsReadOnly());

        return Result.Success(dto);
    }
}
