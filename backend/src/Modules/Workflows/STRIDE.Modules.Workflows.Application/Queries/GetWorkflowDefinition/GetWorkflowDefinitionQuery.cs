using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Queries.GetWorkflowDefinition;

/// <summary>Returns a single workflow definition (with its steps) by ID.</summary>
public sealed record GetWorkflowDefinitionQuery(Guid WorkflowDefinitionId)
    : IRequest<Result<WorkflowDefinitionDto>>;
