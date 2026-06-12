-- Completed workflow instances that have no linked invoice.
-- Ordered by completion time ascending (oldest unbilled first).
-- Capped at 50.
SELECT TOP 50
    wi.Id                                                        AS Id,
    wi.WorkflowName                                              AS WorkflowName,
    c.Name                                                       AS ClientName,
    wi.CompletedAt                                               AS CompletedAt
FROM workflows.WorkflowInstances wi
LEFT JOIN clients.Clients c ON c.Id = wi.ClientId AND c.IsDeleted = 0
WHERE wi.TenantId   = @TenantId
  AND wi.IsDeleted  = 0
  AND wi.Status     = 'Completed'
  AND wi.CompletedAt IS NOT NULL
  AND NOT EXISTS (
      SELECT 1
      FROM invoicing.Invoices inv
      WHERE inv.SourceWorkflowInstanceId = wi.Id
        AND inv.IsDeleted = 0
  )
ORDER BY wi.CompletedAt ASC
