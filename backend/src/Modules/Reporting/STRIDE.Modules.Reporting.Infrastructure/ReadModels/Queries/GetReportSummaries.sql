-- Summary list of saved reports for the tenant, newest first.
-- Column aliases are camelCase-friendly: ReportId → reportId, RequestedBy → requestedBy.
SELECT
    r.Id          AS ReportId,
    r.Name,
    r.ReportType,
    r.GeneratedAt,
    r.RecordCount,
    r.CreatedBy   AS RequestedBy
FROM reporting.Reports r
WHERE r.TenantId  = @TenantId
  AND r.IsDeleted = 0
ORDER BY r.GeneratedAt DESC
