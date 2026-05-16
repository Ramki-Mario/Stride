SELECT
    i.Id,
    i.WorkflowDefinitionId,
    i.WorkflowName,
    i.Status,
    COUNT(s.Id)                                                        AS TotalSteps,
    SUM(CASE WHEN s.Status IN ('Completed','Skipped') THEN 1 ELSE 0 END) AS CompletedSteps,
    i.CreatedAt,
    i.CompletedAt
FROM workflows.WorkflowInstances i
LEFT JOIN workflows.StepInstances s ON s.WorkflowInstanceId = i.Id
WHERE i.TenantId = @TenantId
  AND i.IsDeleted = 0
GROUP BY i.Id, i.WorkflowDefinitionId, i.WorkflowName, i.Status, i.CreatedAt, i.CompletedAt
ORDER BY i.CreatedAt DESC
