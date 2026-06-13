namespace STRIDE.Modules.Webhooks.Application.DTOs;

/// <summary>
/// Read projection of a webhook subscription. Deliberately omits the signing
/// secret — it is returned only once, by the create/regenerate command results.
/// </summary>
public sealed record WebhookSubscriptionDto(
    Guid     Id,
    string   Url,
    IReadOnlyList<string> EventTypes,
    bool     IsActive,
    DateTime CreatedAt);

/// <summary>Returned once when a subscription is created — carries the plaintext secret.</summary>
public sealed record WebhookSecretDto(Guid Id, string SigningSecret);

/// <summary>A subscribable event key plus a human-readable label for the UI.</summary>
public sealed record WebhookEventTypeDto(string Key, string Label);

/// <summary>
/// Read projection of a single webhook delivery attempt, used in the delivery log.
/// ResponseBody is capped at 1 000 chars (truncated at domain level).
/// </summary>
public sealed record WebhookDeliveryDto(
    Guid      Id,
    string    EventType,
    string    Status,           // "Pending" | "Success" | "Failed" | "Exhausted"
    int?      ResponseCode,
    string?   ResponseBody,
    int       AttemptCount,
    DateTime  CreatedAt,
    DateTime? LastAttemptAt,
    DateTime? NextAttemptAt);
