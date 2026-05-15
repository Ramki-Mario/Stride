using STRIDE.BuildingBlocks.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Domain.Entities;

public sealed class StepDefinition : BaseEntity<Guid>
{
    public Guid WorkflowDefinitionId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int Order { get; private set; }
    public bool IsRequired { get; private set; }

    private StepDefinition() { }

    internal static StepDefinition Create(
        Guid workflowDefinitionId,
        string name,
        string? description,
        int order,
        bool isRequired = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new WorkflowDomainException("Step name cannot be empty.");

        if (order < 0)
            throw new WorkflowDomainException("Step order must be a non-negative integer.");

        return new StepDefinition
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = workflowDefinitionId,
            Name = name.Trim(),
            Description = description?.Trim(),
            Order = order,
            IsRequired = isRequired,
        };
    }

    internal void Update(string name, string? description, bool isRequired)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new WorkflowDomainException("Step name cannot be empty.");

        Name = name.Trim();
        Description = description?.Trim();
        IsRequired = isRequired;
    }
}
