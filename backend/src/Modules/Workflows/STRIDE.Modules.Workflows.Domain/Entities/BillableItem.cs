using STRIDE.BuildingBlocks.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Domain.Entities;

/// <summary>
/// A single line of billable work logged when a step instance is completed.
/// Examples: 2.5 hours of labour, 3 replacement parts at £12.50 each, one fixed call-out fee.
/// </summary>
public sealed class BillableItem : BaseEntity<Guid>
{
    public Guid         StepInstanceId { get; private set; }
    public Guid         TenantId       { get; private set; }
    public string       Description    { get; private set; } = string.Empty;
    public decimal      Quantity       { get; private set; }
    public decimal      UnitPrice      { get; private set; }
    public BillableUnit Unit           { get; private set; }
    public DateTime     CreatedAt      { get; private set; }

    /// <summary>Computed line total (Quantity × UnitPrice).</summary>
    public decimal LineTotal => Quantity * UnitPrice;

    private BillableItem() { }

    internal static BillableItem Create(
        Guid         stepInstanceId,
        Guid         tenantId,
        string       description,
        decimal      quantity,
        decimal      unitPrice,
        BillableUnit unit)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new WorkflowDomainException("Billable item description cannot be empty.");

        if (quantity <= 0)
            throw new WorkflowDomainException("Billable item quantity must be greater than zero.");

        if (unitPrice <= 0)
            throw new WorkflowDomainException("Billable item unit price must be greater than zero.");

        return new BillableItem
        {
            Id             = Guid.NewGuid(),
            StepInstanceId = stepInstanceId,
            TenantId       = tenantId,
            Description    = description.Trim(),
            Quantity       = quantity,
            UnitPrice      = unitPrice,
            Unit           = unit,
            CreatedAt      = DateTime.UtcNow,
        };
    }
}
