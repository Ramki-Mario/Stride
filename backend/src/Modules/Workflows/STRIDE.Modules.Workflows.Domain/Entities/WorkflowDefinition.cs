using STRIDE.BuildingBlocks.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Events;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Domain.Entities;

public sealed class WorkflowDefinition : AuditableEntity
{
    private readonly List<StepDefinition> _steps = new();

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public WorkflowStatus Status { get; private set; }

    public IReadOnlyList<StepDefinition> Steps => _steps.AsReadOnly();

    private WorkflowDefinition() { }

    public static WorkflowDefinition Create(
        Guid tenantId,
        string name,
        string? description,
        Guid createdBy)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new WorkflowDomainException("Workflow name cannot be empty.");

        var definition = new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name.Trim(),
            Description = description?.Trim(),
            Status = WorkflowStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
        };

        definition.RaiseDomainEvent(new WorkflowCreatedEvent(definition.Id, tenantId, definition.Name, createdBy));
        return definition;
    }

    public void Update(string name, string? description)
    {
        if (Status != WorkflowStatus.Draft)
            throw new WorkflowDomainException("Only Draft workflows can be edited.");

        if (string.IsNullOrWhiteSpace(name))
            throw new WorkflowDomainException("Workflow name cannot be empty.");

        Name = name.Trim();
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public StepDefinition AddStep(string name, string? description, bool isRequired = true)
    {
        if (Status != WorkflowStatus.Draft)
            throw new WorkflowDomainException("Steps can only be added to Draft workflows.");

        var order = _steps.Count;
        var step = StepDefinition.Create(Id, name, description, order, isRequired);
        _steps.Add(step);
        UpdatedAt = DateTime.UtcNow;
        return step;
    }

    public void UpdateStep(Guid stepId, string name, string? description, bool isRequired)
    {
        if (Status != WorkflowStatus.Draft)
            throw new WorkflowDomainException("Steps can only be edited on Draft workflows.");

        var step = _steps.FirstOrDefault(s => s.Id == stepId)
            ?? throw new WorkflowDomainException($"Step '{stepId}' not found.");

        step.Update(name, description, isRequired);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate(Guid activatedBy)
    {
        if (Status != WorkflowStatus.Draft)
            throw new WorkflowDomainException("Only Draft workflows can be activated.");

        if (_steps.Count == 0)
            throw new WorkflowDomainException("A workflow must have at least one step before activation.");

        Status = WorkflowStatus.Active;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new WorkflowActivatedEvent(Id, TenantId, activatedBy));
    }

    public void Archive()
    {
        if (Status is WorkflowStatus.Running or WorkflowStatus.Paused)
            throw new WorkflowDomainException("Cannot archive a workflow that is currently running or paused.");

        Status = WorkflowStatus.Archived;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Delete(Guid deletedBy)
    {
        if (Status != WorkflowStatus.Draft)
            throw new WorkflowDomainException("Only Draft workflows can be deleted.");

        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
