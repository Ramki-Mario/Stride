-- Average step completion time from assignment to completion, sorted descending.
-- Only assigned + completed steps on completed workflow instances are included.
-- Steps without an AssignedAt are excluded (they have no meaningful duration signal).
SELECT
    si.StepName                                                                        AS StepName,
    AVG(CAST(DATEDIFF(MINUTE, si.AssignedAt, si.CompletedAt) AS FLOAT))               AS AvgDurationMinutes,
    COUNT(*)                                                                           AS OccurrenceCount
FROM [workflows].[StepInstances] si
JOIN [workflows].[WorkflowInstances] wi ON wi.Id = si.WorkflowInstanceId
WHERE wi.TenantId     = @TenantId
  AND wi.Status       = 'Completed'
  AND wi.IsDeleted    = 0
  AND si.Status       = 'Completed'
  AND si.AssignedAt   IS NOT NULL
  AND si.CompletedAt  IS NOT NULL
  AND wi.CompletedAt >= @FromDate
  AND wi.CompletedAt <= @ToDate
  AND (@WorkflowDefinitionId IS NULL OR wi.WorkflowDefinitionId = @WorkflowDefinitionId)
GROUP BY si.StepName
ORDER BY AvgDurationMinutes DESC
