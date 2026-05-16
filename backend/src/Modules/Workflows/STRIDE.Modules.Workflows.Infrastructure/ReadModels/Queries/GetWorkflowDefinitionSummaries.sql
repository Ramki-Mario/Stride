SELECT
    d.Id,
    d.Name,
    d.Description,
    d.Status,
    COUNT(s.Id) AS StepCount,
    d.CreatedAt,
    d.UpdatedAt
FROM workflows.WorkflowDefinitions d
LEFT JOIN workflows.StepDefinitions s ON s.WorkflowDefinitionId = d.Id
WHERE d.TenantId = @TenantId
  AND d.IsDeleted = 0
GROUP BY d.Id, d.Name, d.Description, d.Status, d.CreatedAt, d.UpdatedAt
ORDER BY d.UpdatedAt DESC
