using STRIDE.Modules.Workflows.Domain.Enums;

namespace STRIDE.Modules.Workflows.API.Dtos;

/// <summary>Body for POST .../steps/{stepId}/assign</summary>
public sealed record AssignStepRequest
{
    public required Guid AssigneeId { get; init; }
}

/// <summary>Body for POST .../steps/{stepId}/fail</summary>
public sealed record FailStepRequest(string Reason);

/// <summary>
/// Optional body for POST .../steps/{stepId}/complete
/// When omitted, the step is completed with no billable items.
/// </summary>
public sealed record CompleteStepRequest(
    IReadOnlyList<BillableItemRequest>? BillableItems = null);

/// <summary>A single billable line item submitted on step completion.</summary>
public sealed record BillableItemRequest(
    string       Description,
    decimal      Quantity,
    decimal      UnitPrice,
    BillableUnit Unit);
