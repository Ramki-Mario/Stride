-- Summary list of saved reports for the tenant, newest first.
SELECT
    r.Id,
    r.Name,
    r.ReportType,
    r.GeneratedAt,
    r.RecordCount
FROM reporting.Reports r
WHERE r.TenantId  = @TenantId
  AND r.IsDeleted = 0
ORDER BY r.GeneratedAt DESC
