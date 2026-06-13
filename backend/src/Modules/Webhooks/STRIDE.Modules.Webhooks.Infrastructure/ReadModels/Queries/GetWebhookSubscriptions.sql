-- Active (non-deleted) webhook subscriptions for a tenant, newest first.
-- The signing secret column is deliberately NOT selected — secrets are shown
-- only once on creation/regeneration, never on read.
SELECT
    s.Id,
    s.Url,
    s.EventTypesJson,
    s.IsActive,
    s.CreatedAt
FROM [webhooks].[WebhookSubscriptions] s
WHERE s.TenantId  = @TenantId
  AND s.IsDeleted = 0
ORDER BY s.CreatedAt DESC;
