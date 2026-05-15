using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Commands.UpdateWorkflow;

/// <summary>
/// Updates the name, description, and steps of a Draft workflow definition.
/// Only Draft workflows are editable; Active/Running definitions are immutable.
/// </summary>
public sealed record UpdateWorkflowCommand(
    Guid WorkflowDefinitionId,
    string Name,
    string? Description,
    Guid UpdatedBy) : IRequest<Result>;
