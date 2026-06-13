using System.Security.Cryptography;
using System.Text.Json;
using STRIDE.BuildingBlocks.Domain.Entities;
using STRIDE.Modules.Webhooks.Domain.Events;
using STRIDE.Modules.Webhooks.Domain.Exceptions;

namespace STRIDE.Modules.Webhooks.Domain.Entities;

/// <summary>
/// A tenant-registered outbound webhook endpoint.
///
/// Own aggregate root: a subscription has an independent lifecycle (created,
/// edited, secret-rotated, deactivated, deleted) and is the unit external
/// integrations are configured against. The <see cref="SigningSecret"/> is held
/// in plaintext in memory but is encrypted at rest by an EF value converter — the
/// domain stays unaware of the encryption mechanism (a persistence concern).
///
/// Subscribed <see cref="EventTypes"/> are persisted as a JSON array string
/// (mirrors the StepFieldDefinition.DropdownOptionsJson pattern) so the column
/// maps trivially without a collection table.
/// </summary>
public sealed class WebhookSubscription : AuditableEntity
{
    /// <summary>Prefix on generated secrets so integrators can recognise the credential type at a glance.</summary>
    private const string SecretPrefix = "whsec_";

    public string Url            { get; private set; } = string.Empty;
    public string SigningSecret  { get; private set; } = string.Empty;
    public string EventTypesJson { get; private set; } = "[]";
    public bool   IsActive       { get; private set; }

    /// <summary>The subscribed event keys, materialised from the persisted JSON array.</summary>
    public IReadOnlyList<string> EventTypes =>
        string.IsNullOrWhiteSpace(EventTypesJson)
            ? Array.Empty<string>()
            : JsonSerializer.Deserialize<List<string>>(EventTypesJson) ?? new List<string>();

    // EF Core constructor
    private WebhookSubscription() { }

    // ── Factory ─────────────────────────────────────────────────────────────

    public static WebhookSubscription Create(
        Guid tenantId, string url, IReadOnlyList<string> eventTypes, Guid createdBy)
    {
        var cleanUrl    = ValidateUrl(url);
        var cleanEvents = NormalizeEventTypes(eventTypes);
        var now         = DateTime.UtcNow;

        var sub = new WebhookSubscription
        {
            Id             = Guid.NewGuid(),
            TenantId       = tenantId,
            Url            = cleanUrl,
            SigningSecret  = GenerateSecret(),
            EventTypesJson = JsonSerializer.Serialize(cleanEvents),
            IsActive       = true,
            CreatedAt      = now,
            UpdatedAt      = now,
            CreatedBy      = createdBy,
        };

        sub.RaiseDomainEvent(new WebhookSubscriptionCreatedEvent(sub.Id, tenantId, cleanUrl, createdBy));
        return sub;
    }

    // ── Update ──────────────────────────────────────────────────────────────

    public void Update(string url, IReadOnlyList<string> eventTypes, bool isActive, Guid updatedBy)
    {
        Url            = ValidateUrl(url);
        EventTypesJson = JsonSerializer.Serialize(NormalizeEventTypes(eventTypes));
        IsActive       = isActive;
        UpdatedAt      = DateTime.UtcNow;

        RaiseDomainEvent(new WebhookSubscriptionUpdatedEvent(Id, TenantId, updatedBy));
    }

    // ── Secret rotation ───────────────────────────────────────────────────────

    /// <summary>Rotates the signing secret and returns the new plaintext value (shown to the user once).</summary>
    public string RegenerateSecret(Guid regeneratedBy)
    {
        SigningSecret = GenerateSecret();
        UpdatedAt     = DateTime.UtcNow;

        RaiseDomainEvent(new WebhookSecretRegeneratedEvent(Id, TenantId, regeneratedBy));
        return SigningSecret;
    }

    // ── Soft delete ───────────────────────────────────────────────────────────

    public void Delete(Guid deletedBy)
    {
        if (IsDeleted)
            throw new WebhookDomainException("Webhook subscription is already deleted.");

        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new WebhookSubscriptionDeletedEvent(Id, TenantId, deletedBy));
    }

    // ── Invariants ──────────────────────────────────────────────────────────

    private static string ValidateUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new WebhookDomainException("Webhook URL is required.");

        var trimmed = url.Trim();

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps)
            throw new WebhookDomainException("Webhook URL must be a valid absolute HTTPS URL.");

        return trimmed;
    }

    private static List<string> NormalizeEventTypes(IReadOnlyList<string> eventTypes)
    {
        if (eventTypes is null || eventTypes.Count == 0)
            throw new WebhookDomainException("At least one event type must be selected.");

        var cleaned = eventTypes
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(e => e.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (cleaned.Count == 0)
            throw new WebhookDomainException("At least one event type must be selected.");

        var unknown = cleaned.Where(e => !WebhookEventTypes.IsKnown(e)).ToList();
        if (unknown.Count > 0)
            throw new WebhookDomainException($"Unknown event type(s): {string.Join(", ", unknown)}.");

        return cleaned;
    }

    private static string GenerateSecret() =>
        SecretPrefix + Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
}
