namespace STRIDE.Modules.Administration.Domain.Entities;

public sealed record AuditLogData(
    Guid    TenantId,
    Guid    ActorId,
    string  ActorEmail,
    string  Action,
    string  ResourceType,
    Guid?   ResourceId   = null,
    string? OldValueJson = null,
    string? NewValueJson = null);

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

    public static AuditLog Create(AuditLogData data)
        => new()
        {
            Id           = Guid.NewGuid(),
            TenantId     = data.TenantId,
            ActorId      = data.ActorId,
            ActorEmail   = data.ActorEmail,
            Action       = data.Action,
            ResourceType = data.ResourceType,
            ResourceId   = data.ResourceId,
            OldValueJson = data.OldValueJson,
            NewValueJson = data.NewValueJson,
            Timestamp    = DateTime.UtcNow,
        };
}
