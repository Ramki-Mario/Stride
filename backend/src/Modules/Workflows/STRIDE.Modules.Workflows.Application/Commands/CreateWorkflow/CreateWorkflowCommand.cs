using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Domain.Enums;

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
    IReadOnlyList<StepRequest> Steps,
    decimal? SlaOffsetHours = null) : IRequest<Result<CreateWorkflowResult>>;

/// <summary>One step in a create-workflow command.</summary>
public sealed record StepRequest(
    string Name,
    string? Description,
    bool IsRequired = true,
    Guid? RequiredRoleId = null,
    IReadOnlyList<FieldDefinitionRequest>? FieldDefinitions = null,
    decimal? DueOffsetHours = null);

/// <summary>
/// Defines a single data-capture field on a step.
/// Matches <see cref="Domain.Entities.StepFieldDefinition"/> design-time schema.
/// </summary>
public sealed record FieldDefinitionRequest(
    string Label,
    StepFieldType FieldType,
    bool IsRequired,
    string? HelpText = null,
    IReadOnlyList<string>? DropdownOptions = null);
