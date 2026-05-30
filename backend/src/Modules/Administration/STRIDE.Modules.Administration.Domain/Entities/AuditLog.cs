namespace STRIDE.Modules.Administration.Domain.Entities;

/// <summary>
/// Immutable audit trail record — never updated or soft-deleted.
/// Captures who did what to which resource and when, within a tenant.
/// </summary>
public sealed class AuditLog
{
    private AuditLog() { }  // EF Core constructor

    public Guid    Id           { get; private set; }
    public Guid    TenantId     { get; private set; }
    public Guid    ActorId      { get; private set; }
    public string  ActorEmail   { get; private set; } = string.Empty;
    public string  Action       { get; private set; } = string.Empty;
    public string  ResourceType { get; private set; } = string.Empty;
    public Guid?   ResourceId   { get; private set; }
    public string? OldValueJson { get; private set; }
    public string? NewValueJson { get; private set; }
    public DateTime Timestamp   { get; private set; }

    public static AuditLog Create(
        Guid    tenantId,
        Guid    actorId,
        string  actorEmail,
        string  action,
        string  resourceType,
        Guid?   resourceId   = null,
        string? oldValueJson = null,
        string? newValueJson = null)
        => new()
        {
            Id           = Guid.NewGuid(),
            TenantId     = tenantId,
            ActorId      = actorId,
            ActorEmail   = actorEmail,
            Action       = action,
            ResourceType = resourceType,
            ResourceId   = resourceId,
            OldValueJson = oldValueJson,
            NewValueJson = newValueJson,
            Timestamp    = DateTime.UtcNow,
        };
}
