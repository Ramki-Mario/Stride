-- Per-definition instance counts broken down by state, for the Workflow Summary report.
-- Reads from the workflows schema (cross-schema read — shared DB, ADR-006/ADR-012).
SELECT
    d.Name                                                              AS WorkflowName,
    d.Status,
    COUNT(i.Id)                                                         AS TotalInstances,
    SUM(CASE WHEN i.Status = 'Running'   THEN 1 ELSE 0 END)            AS RunningInstances,
    SUM(CASE WHEN i.Status = 'Completed' THEN 1 ELSE 0 END)            AS CompletedInstances,
    SUM(CASE WHEN i.Status = 'Failed'    THEN 1 ELSE 0 END)            AS FailedInstances
FROM workflows.WorkflowDefinitions d
LEFT JOIN workflows.WorkflowInstances i
       ON i.WorkflowDefinitionId = d.Id
      AND i.IsDeleted             = 0
WHERE d.TenantId  = @TenantId
  AND d.IsDeleted = 0
GROUP BY d.Id, d.Name, d.Status
ORDER BY d.Name ASC
