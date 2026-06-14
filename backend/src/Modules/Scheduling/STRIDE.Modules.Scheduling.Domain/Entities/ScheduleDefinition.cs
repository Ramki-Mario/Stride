using STRIDE.BuildingBlocks.Domain.Entities;
using STRIDE.Modules.Scheduling.Domain.Events;
using STRIDE.Modules.Scheduling.Domain.Exceptions;
using STRIDE.Modules.Scheduling.Domain.ValueObjects;

namespace STRIDE.Modules.Scheduling.Domain.Entities;

public sealed class ScheduleDefinition : AuditableEntity
{
    public const int NameMaxLength        = 100;
    public const int DescriptionMaxLength = 500;

    public string          Name                 { get; private set; } = string.Empty;
    public string?         Description          { get; private set; }
    public Guid            WorkflowDefinitionId { get; private set; }
    public CronExpression  CronExpression       { get; private set; } = null!;
    public bool            IsActive             { get; private set; }
    public DateTime?       NextRunAt            { get; private set; }

    private ScheduleDefinition() { }

    public static ScheduleDefinition Create(
        Guid            tenantId,
        string          name,
        string?         description,
        Guid            workflowDefinitionId,
        CronExpression  cronExpression,
        bool            isActive,
        DateTime?       nextRunAt,
        Guid            createdBy)
    {
        ValidateName(name);

        var now = DateTime.UtcNow;
        var schedule = new ScheduleDefinition
        {
            Id                   = Guid.NewGuid(),
            TenantId             = tenantId,
            Name                 = name.Trim(),
            Description          = description?.Trim(),
            WorkflowDefinitionId = workflowDefinitionId,
            CronExpression       = cronExpression,
            IsActive             = isActive,
            NextRunAt            = nextRunAt,
            CreatedAt            = now,
            UpdatedAt            = now,
            CreatedBy            = createdBy,
        };

        schedule.RaiseDomainEvent(new ScheduleDefinitionCreatedEvent(
            schedule.Id, tenantId, schedule.Name, workflowDefinitionId, createdBy));

        return schedule;
    }

    public void Update(
        string         name,
        string?        description,
        Guid           workflowDefinitionId,
        CronExpression cronExpression,
        DateTime?      nextRunAt,
        Guid           updatedBy)
    {
        if (IsDeleted)
            throw new SchedulingDomainException("A deleted schedule cannot be updated.");

        ValidateName(name);

        Name                 = name.Trim();
        Description          = description?.Trim();
        WorkflowDefinitionId = workflowDefinitionId;
        CronExpression       = cronExpression;
        NextRunAt            = nextRunAt;
        UpdatedAt            = DateTime.UtcNow;
    }

    public void Activate(DateTime? nextRunAt, Guid activatedBy)
    {
        if (IsDeleted)
            throw new SchedulingDomainException("A deleted schedule cannot be activated.");

        if (IsActive)
            throw new SchedulingDomainException("Schedule is already active.");

        IsActive  = true;
        NextRunAt = nextRunAt;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ScheduleActivatedEvent(Id, TenantId, activatedBy));
    }

    public void Deactivate(Guid deactivatedBy)
    {
        if (IsDeleted)
            throw new SchedulingDomainException("A deleted schedule cannot be deactivated.");

        if (!IsActive)
            throw new SchedulingDomainException("Schedule is already inactive.");

        IsActive  = false;
        NextRunAt = null;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ScheduleDeactivatedEvent(Id, TenantId, deactivatedBy));
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        IsActive  = false;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new SchedulingDomainException("Schedule name cannot be empty.");

        if (name.Trim().Length > NameMaxLength)
            throw new SchedulingDomainException($"Schedule name cannot exceed {NameMaxLength} characters.");
    }
}
