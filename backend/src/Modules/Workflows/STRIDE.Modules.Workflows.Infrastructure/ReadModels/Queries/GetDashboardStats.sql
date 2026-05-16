SELECT
    COUNT(*)                                                      AS TotalDefinitions,
    SUM(CASE WHEN d.Status = 'Active'    THEN 1 ELSE 0 END)      AS ActiveDefinitions,
    (SELECT COUNT(*) FROM workflows.WorkflowInstances i
     WHERE i.TenantId = @TenantId AND i.IsDeleted = 0
       AND i.Status = 'Running')                                  AS TotalRunning,
    (SELECT COUNT(*) FROM workflows.WorkflowInstances i
     WHERE i.TenantId = @TenantId AND i.IsDeleted = 0
       AND i.Status = 'Completed')                                AS TotalCompleted,
    (SELECT COUNT(*) FROM workflows.WorkflowInstances i
     WHERE i.TenantId = @TenantId AND i.IsDeleted = 0
       AND i.Status = 'Failed')                                   AS TotalFailed,
    (SELECT COUNT(*) FROM workflows.WorkflowInstances i
     WHERE i.TenantId = @TenantId AND i.IsDeleted = 0
       AND i.Status = 'Cancelled')                                AS TotalCancelled
FROM workflows.WorkflowDefinitions d
WHERE d.TenantId = @TenantId
  AND d.IsDeleted = 0
