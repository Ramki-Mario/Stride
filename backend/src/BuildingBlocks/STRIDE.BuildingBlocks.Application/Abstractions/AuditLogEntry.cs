namespace STRIDE.BuildingBlocks.Application.Abstractions;

public sealed record AuditLogEntry(
    Guid    TenantId,
    Guid    ActorId,
    string  ActorEmail,
    string  Action,
    string  ResourceType,
    Guid?   ResourceId   = null,
    string? OldValueJson = null,
    string? NewValueJson = null);
