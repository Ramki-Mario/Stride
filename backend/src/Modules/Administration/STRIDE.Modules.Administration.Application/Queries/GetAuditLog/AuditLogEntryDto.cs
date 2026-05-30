namespace STRIDE.Modules.Administration.Application.Queries.GetAuditLog;

public sealed record AuditLogEntryDto(
    Guid     Id,
    Guid     ActorId,
    string   ActorEmail,
    string   Action,
    string   ResourceType,
    Guid?    ResourceId,
    string?  OldValueJson,
    string?  NewValueJson,
    DateTime Timestamp);
