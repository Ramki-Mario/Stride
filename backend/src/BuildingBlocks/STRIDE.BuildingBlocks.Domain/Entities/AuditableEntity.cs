using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.BuildingBlocks.Domain.Entities;

public abstract class AuditableEntity : BaseEntity<Guid>
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public Guid TenantId { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime UpdatedAt { get; protected set; }
    public Guid CreatedBy { get; protected set; }
    public bool IsDeleted { get; protected set; }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
