SELECT
    i.Id,
    i.WorkflowDefinitionId,
    i.WorkflowName,
    i.Status,
    COUNT(s.Id)                                                        AS TotalSteps,
    SUM(CASE WHEN s.Status IN ('Completed','Skipped') THEN 1 ELSE 0 END) AS CompletedSteps,
    i.CreatedAt,
    i.CompletedAt,
    i.TeamId,
    tm.Name                                                            AS TeamName
FROM workflows.WorkflowInstances i
LEFT JOIN workflows.StepInstances s ON s.WorkflowInstanceId = i.Id
LEFT JOIN teams.Teams tm ON tm.Id = i.TeamId AND tm.IsDeleted = 0
WHERE i.TenantId = @TenantId
  AND i.IsDeleted = 0
  AND i.WorkflowDefinitionId = @DefinitionId
GROUP BY i.Id, i.WorkflowDefinitionId, i.WorkflowName, i.Status, i.CreatedAt, i.CompletedAt,
         i.TeamId, tm.Name
ORDER BY i.CreatedAt DESC
