SELECT COUNT(*)
FROM [administration].[AuditLogs]
WHERE TenantId = @TenantId
  AND (@Action IS NULL OR Action = @Action)
  AND (@From   IS NULL OR Timestamp >= @From)
  AND (@To     IS NULL OR Timestamp <= @To);
