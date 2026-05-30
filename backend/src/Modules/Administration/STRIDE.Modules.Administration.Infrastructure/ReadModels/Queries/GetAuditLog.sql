SELECT
    Id,
    ActorId,
    ActorEmail,
    Action,
    ResourceType,
    ResourceId,
    OldValueJson,
    NewValueJson,
    Timestamp
FROM [administration].[AuditLogs]
WHERE TenantId = @TenantId
  AND (@Action IS NULL OR Action = @Action)
  AND (@From   IS NULL OR Timestamp >= @From)
  AND (@To     IS NULL OR Timestamp <= @To)
ORDER BY Timestamp DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
