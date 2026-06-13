-- Flat list of completed workflow instances for CSV export.
-- Ordered newest first.
SELECT
    wi.Id                                                                        AS InstanceId,
    wi.WorkflowName                                                              AS WorkflowName,
    wi.CreatedAt                                                                 AS StartedAt,
    wi.CompletedAt                                                               AS CompletedAt,
    CAST(DATEDIFF(MINUTE, wi.CreatedAt, wi.CompletedAt) AS FLOAT)               AS DurationMinutes
FROM [workflows].[WorkflowInstances] wi
WHERE wi.TenantId    = @TenantId
  AND wi.Status      = 'Completed'
  AND wi.IsDeleted   = 0
  AND wi.CompletedAt >= @FromDate
  AND wi.CompletedAt <= @ToDate
  AND (@WorkflowDefinitionId IS NULL OR wi.WorkflowDefinitionId = @WorkflowDefinitionId)
ORDER BY wi.CompletedAt DESC
