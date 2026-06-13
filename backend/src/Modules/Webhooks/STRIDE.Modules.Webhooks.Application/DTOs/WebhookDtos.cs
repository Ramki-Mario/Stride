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
