-- Active (non-archived) workflow definitions for the analytics filter dropdown.
SELECT
    d.Id    AS Id,
    d.Name  AS Name
FROM [workflows].[WorkflowDefinitions] d
WHERE d.TenantId  = @TenantId
  AND d.IsDeleted = 0
  AND d.Status   != 'Archived'
ORDER BY d.Name ASC
