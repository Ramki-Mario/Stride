-- Average workflow completion time grouped by definition.
-- Duration = minutes from instance creation to CompletedAt.
-- Only 'Completed' instances within the given date range are included.
SELECT
    wi.WorkflowDefinitionId                                                  AS DefinitionId,
    MAX(wi.WorkflowName)                                                     AS DefinitionName,
    AVG(CAST(DATEDIFF(MINUTE, wi.CreatedAt, wi.CompletedAt) AS FLOAT))      AS AvgDurationMinutes,
    COUNT(*)                                                                 AS InstanceCount
FROM [workflows].[WorkflowInstances] wi
WHERE wi.TenantId    = @TenantId
  AND wi.Status      = 'Completed'
  AND wi.IsDeleted   = 0
  AND wi.CompletedAt >= @FromDate
  AND wi.CompletedAt <= @ToDate
  AND (@WorkflowDefinitionId IS NULL OR wi.WorkflowDefinitionId = @WorkflowDefinitionId)
GROUP BY wi.WorkflowDefinitionId
ORDER BY AvgDurationMinutes DESC
