using STRIDE.BuildingBlocks.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Domain.Entities;

/// <summary>
/// Captured runtime value for a field definition when a step instance is completed.
/// Values are stored as strings so field-type-specific parsing can evolve without
/// changing the persistence shape.
/// </summary>
public sealed class StepFieldValue : BaseEntity<Guid>
{
    public Guid     StepInstanceId        { get; private set; }
    public Guid     StepFieldDefinitionId { get; private set; }
    public Guid     TenantId              { get; private set; }
    public string   Value                 { get; private set; } = string.Empty;
    public DateTime CreatedAt             { get; private set; }

    private StepFieldValue() { }

    internal static StepFieldValue Create(
        Guid stepInstanceId,
        Guid tenantId,
        Guid stepFieldDefinitionId,
        string value)
    {
        if (stepFieldDefinitionId == Guid.Empty)
            throw new WorkflowDomainException("Step field definition id is required.");

        return new StepFieldValue
        {
            Id                    = Guid.NewGuid(),
            StepInstanceId        = stepInstanceId,
            TenantId              = tenantId,
            StepFieldDefinitionId = stepFieldDefinitionId,
            Value                 = value,
            CreatedAt             = DateTime.UtcNow,
        };
    }
}
