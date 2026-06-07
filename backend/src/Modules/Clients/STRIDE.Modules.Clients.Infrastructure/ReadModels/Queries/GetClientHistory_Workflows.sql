-- Returns workflow instances linked to the given client (cross-schema read).
SELECT
    wi.Id          AS Id,
    wi.WorkflowName AS WorkflowName,
    wi.Status       AS Status,
    wi.CreatedAt    AS CreatedAt,
    wi.CompletedAt  AS CompletedAt
FROM workflows.WorkflowInstances wi
WHERE wi.TenantId  = @TenantId
  AND wi.ClientId  = @ClientId
  AND wi.IsDeleted = 0
ORDER BY wi.CreatedAt DESC;
