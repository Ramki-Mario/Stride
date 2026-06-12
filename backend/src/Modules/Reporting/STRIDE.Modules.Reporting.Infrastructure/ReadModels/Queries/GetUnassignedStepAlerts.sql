-- Step instances that are active (Pending/InProgress) with no assignee,
-- belonging to workflow instances started more than one hour ago.
-- Capped at 50, ordered longest-waiting first.
SELECT TOP 50
    si.Id                                                        AS StepId,
    si.StepName                                                  AS StepName,
    wi.Id                                                        AS WorkflowInstanceId,
    wi.WorkflowName                                              AS WorkflowName,
    DATEDIFF(MINUTE, wi.CreatedAt, GETUTCDATE())                AS MinutesWaiting
FROM workflows.StepInstances si
INNER JOIN workflows.WorkflowInstances wi
    ON wi.Id       = si.WorkflowInstanceId
   AND wi.IsDeleted = 0
   AND wi.TenantId = @TenantId
   AND wi.Status   = 'Running'
WHERE si.TenantId   = @TenantId
  AND si.AssigneeId IS NULL
  AND si.Status     IN ('Pending', 'InProgress')
  AND wi.CreatedAt  < DATEADD(HOUR, -1, GETUTCDATE())
ORDER BY wi.CreatedAt ASC
