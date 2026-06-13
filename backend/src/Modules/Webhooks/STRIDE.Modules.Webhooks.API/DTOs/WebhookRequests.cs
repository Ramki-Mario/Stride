namespace STRIDE.Modules.Webhooks.API.DTOs;

public sealed record CreateWebhookSubscriptionRequest(
    string                Url,
    IReadOnlyList<string> EventTypes);

public sealed record UpdateWebhookSubscriptionRequest(
    string                Url,
    IReadOnlyList<string> EventTypes,
    bool                  IsActive);
