-- Workflow instances that are still Running and have a deadline within the next 4 hours.
-- Ordered by soonest deadline first (most urgent at top).
-- Capped at 50.
SELECT TOP 50
    wi.Id                                                        AS Id,
    wi.WorkflowName                                              AS WorkflowName,
    c.Name                                                       AS ClientName,
    DATEDIFF(MINUTE, GETUTCDATE(), wi.DeadlineAt)               AS MinutesRemaining
FROM workflows.WorkflowInstances wi
LEFT JOIN clients.Clients c ON c.Id = wi.ClientId AND c.IsDeleted = 0
WHERE wi.TenantId   = @TenantId
  AND wi.IsDeleted  = 0
  AND wi.Status     = 'Running'
  AND wi.DeadlineAt IS NOT NULL
  AND wi.DeadlineAt >= GETUTCDATE()
  AND wi.DeadlineAt <= DATEADD(HOUR, 4, GETUTCDATE())
ORDER BY wi.DeadlineAt ASC
