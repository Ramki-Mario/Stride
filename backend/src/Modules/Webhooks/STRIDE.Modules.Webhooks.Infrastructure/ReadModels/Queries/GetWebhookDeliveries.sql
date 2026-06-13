-- Last N deliveries for a subscription, newest first.
SELECT TOP (@Limit)
    d.Id,
    d.EventType,
    d.Status,
    d.ResponseCode,
    d.ResponseBody,
    d.AttemptCount,
    d.CreatedAt,
    d.LastAttemptAt,
    d.NextAttemptAt
FROM [webhooks].[WebhookDeliveries] d
WHERE d.TenantId              = @TenantId
  AND d.WebhookSubscriptionId = @SubscriptionId
  AND d.IsDeleted             = 0
ORDER BY d.CreatedAt DESC;
