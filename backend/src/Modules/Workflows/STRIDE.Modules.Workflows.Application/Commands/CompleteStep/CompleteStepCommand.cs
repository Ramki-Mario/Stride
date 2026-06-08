using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Domain.Enums;

namespace STRIDE.Modules.Workflows.Application.Commands.CompleteStep;

/// <summary>
/// Marks a step instance as Completed. After completion, <c>WorkflowInstance</c>
/// automatically checks if all steps are terminal and completes the workflow if so.
/// </summary>
public sealed record CompleteStepCommand(
    Guid WorkflowInstanceId,
    Guid StepInstanceId,
    Guid CompletedBy,
    IReadOnlyList<BillableItemInput>? BillableItems = null,
    IReadOnlyList<FieldValueInput>? FieldValues = null) : IRequest<Result>;

/// <summary>
/// A single billable line item passed in from the API when completing a step.
/// </summary>
public sealed record BillableItemInput(
    string      Description,
    decimal     Quantity,
    decimal     UnitPrice,
    BillableUnit Unit);

/// <summary>
/// A single captured field value submitted when completing a step.
/// </summary>
public sealed record FieldValueInput(
    Guid StepFieldDefinitionId,
    string Value);
