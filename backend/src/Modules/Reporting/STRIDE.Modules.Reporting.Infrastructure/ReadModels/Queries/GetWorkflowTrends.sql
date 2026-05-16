-- Daily workflow activity trend for the last @Days days.
-- Groups WorkflowInstances by creation date to produce a time series.
SELECT
    CAST(i.CreatedAt AS DATE)                                              AS TrendDate,
    COUNT(*)                                                               AS Started,
    SUM(CASE WHEN i.Status = 'Completed' THEN 1 ELSE 0 END)               AS Completed,
    SUM(CASE WHEN i.Status = 'Failed'    THEN 1 ELSE 0 END)               AS Failed
FROM workflows.WorkflowInstances i
WHERE i.TenantId  = @TenantId
  AND i.IsDeleted = 0
  AND i.CreatedAt >= DATEADD(DAY, -@Days, GETUTCDATE())
GROUP BY CAST(i.CreatedAt AS DATE)
ORDER BY TrendDate ASC
