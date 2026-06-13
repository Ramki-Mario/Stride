-- Single non-deleted webhook subscription by id, scoped to the tenant.
-- Signing secret deliberately omitted (shown only on creation/regeneration).
SELECT
    s.Id,
    s.Url,
    s.EventTypesJson,
    s.IsActive,
    s.CreatedAt
FROM [webhooks].[WebhookSubscriptions] s
WHERE s.TenantId  = @TenantId
  AND s.Id        = @SubscriptionId
  AND s.IsDeleted = 0;
