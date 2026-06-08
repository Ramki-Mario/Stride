SELECT
    si.Id              AS StepInstanceId,
    si.StepName,
    si.Status          AS StepStatus,
    si.AssignedAt,
    wi.Id              AS WorkflowInstanceId,
    wi.WorkflowName,
    wi.Status          AS WorkflowStatus,
    c.Name             AS ClientName
FROM   [workflows].[StepInstances]    si
JOIN   [workflows].[WorkflowInstances] wi ON wi.Id = si.WorkflowInstanceId
LEFT JOIN [clients].[Clients]         c  ON c.Id  = wi.ClientId
                                         AND c.IsDeleted = 0
WHERE  si.TenantId   = @TenantId
  AND  si.AssigneeId = @UserId
  AND  si.Status NOT IN ('Completed', 'Skipped', 'Failed')
  AND  wi.Status NOT IN ('Completed', 'Cancelled', 'Failed', 'Archived')
  AND  wi.IsDeleted  = 0
ORDER BY si.AssignedAt DESC
