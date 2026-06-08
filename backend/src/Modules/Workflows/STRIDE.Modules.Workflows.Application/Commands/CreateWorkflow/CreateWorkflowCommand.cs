using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Commands.CreateWorkflow;

/// <summary>
/// Creates a new workflow definition in Draft status for the current tenant.
/// Steps can be added as part of the initial creation via <see cref="StepRequest"/>.
/// </summary>
public sealed record CreateWorkflowCommand(
    Guid TenantId,
    string Name,
    string? Description,
    Guid CreatedBy,
    IReadOnlyList<StepRequest> Steps) : IRequest<Result<CreateWorkflowResult>>;

public sealed record StepRequest(
    string Name,
    string? Description,
    bool IsRequired = true,
    Guid? RequiredRoleId = null);
