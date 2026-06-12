-- Workflow instances that are still Running but have passed their SLA deadline.
-- Capped at 50, ordered most overdue first.
SELECT TOP 50
    wi.Id                                                        AS Id,
    wi.WorkflowName                                              AS WorkflowName,
    c.Name                                                       AS ClientName,
    DATEDIFF(MINUTE, wi.DeadlineAt, GETUTCDATE())               AS MinutesOverdue
FROM workflows.WorkflowInstances wi
LEFT JOIN clients.Clients c ON c.Id = wi.ClientId AND c.IsDeleted = 0
WHERE wi.TenantId  = @TenantId
  AND wi.IsDeleted = 0
  AND wi.Status    = 'Running'
  AND wi.DeadlineAt IS NOT NULL
  AND wi.DeadlineAt < GETUTCDATE()
ORDER BY wi.DeadlineAt ASC
