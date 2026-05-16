-- Dashboard KPI snapshot: definition counts + instance state counts for the tenant.
-- Reads directly from the workflows schema (cross-schema read, shared DB strategy).
SELECT
    COUNT(*)                                                          AS TotalDefinitions,
    SUM(CASE WHEN d.Status = 'Active'    THEN 1 ELSE 0 END)          AS ActiveDefinitions,
    (SELECT COUNT(*) FROM workflows.WorkflowInstances i
     WHERE i.TenantId = @TenantId AND i.IsDeleted = 0
       AND i.Status = 'Running')                                      AS RunningInstances,
    (SELECT COUNT(*) FROM workflows.WorkflowInstances i
     WHERE i.TenantId = @TenantId AND i.IsDeleted = 0
       AND i.Status = 'Completed')                                    AS CompletedInstances,
    (SELECT COUNT(*) FROM workflows.WorkflowInstances i
     WHERE i.TenantId = @TenantId AND i.IsDeleted = 0
       AND i.Status = 'Failed')                                       AS FailedInstances,
    (SELECT COUNT(*) FROM workflows.WorkflowInstances i
     WHERE i.TenantId = @TenantId AND i.IsDeleted = 0
       AND i.Status = 'Cancelled')                                    AS CancelledInstances
FROM workflows.WorkflowDefinitions d
WHERE d.TenantId = @TenantId
  AND d.IsDeleted = 0
