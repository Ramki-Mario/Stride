-- Average completion time per calendar week (Monday-aligned).
-- Produces a time series for the trend chart.
SELECT
    CAST(DATEADD(WEEK, DATEDIFF(WEEK, 0, wi.CompletedAt), 0) AS DATE)      AS WeekStart,
    AVG(CAST(DATEDIFF(MINUTE, wi.CreatedAt, wi.CompletedAt) AS FLOAT))     AS AvgDurationMinutes,
    COUNT(*)                                                                AS InstanceCount
FROM [workflows].[WorkflowInstances] wi
WHERE wi.TenantId    = @TenantId
  AND wi.Status      = 'Completed'
  AND wi.IsDeleted   = 0
  AND wi.CompletedAt >= @FromDate
  AND wi.CompletedAt <= @ToDate
  AND (@WorkflowDefinitionId IS NULL OR wi.WorkflowDefinitionId = @WorkflowDefinitionId)
GROUP BY CAST(DATEADD(WEEK, DATEDIFF(WEEK, 0, wi.CompletedAt), 0) AS DATE)
ORDER BY WeekStart ASC
