using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Queries.GetWorkflowInstance;

/// <summary>Returns a single workflow instance (with its step instances) by ID.</summary>
public sealed record GetWorkflowInstanceQuery(Guid WorkflowInstanceId)
    : IRequest<Result<WorkflowInstanceDto>>;
