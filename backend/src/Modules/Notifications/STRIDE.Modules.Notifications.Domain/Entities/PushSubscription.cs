using STRIDE.BuildingBlocks.Domain.Entities;

namespace STRIDE.Modules.Notifications.Domain.Entities;

/// <summary>
/// Stores a Web Push subscription for a user so the server can send push notifications
/// to their browser even when they are not actively using the app.
/// One user may have multiple active subscriptions (phone, desktop, tablet).
/// </summary>
public sealed class PushSubscription : BaseEntity<Guid>
{
    public Guid TenantId   { get; private set; }
    public Guid UserId     { get; private set; }

    /// <summary>The push service endpoint URL provided by the browser.</summary>
    public string Endpoint { get; private set; } = string.Empty;

    /// <summary>Base64url-encoded P-256 DH public key for payload encryption.</summary>
    public string P256dh   { get; private set; } = string.Empty;

    /// <summary>Base64url-encoded authentication secret for the subscription.</summary>
    public string Auth     { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    // EF Core
    private PushSubscription() { }

    public static PushSubscription Create(
        Guid tenantId,
        Guid userId,
        string endpoint,
        string p256dh,
        string auth)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(p256dh);
        ArgumentException.ThrowIfNullOrWhiteSpace(auth);

        return new PushSubscription
        {
            Id        = Guid.NewGuid(),
            TenantId  = tenantId,
            UserId    = userId,
            Endpoint  = endpoint,
            P256dh    = p256dh,
            Auth      = auth,
            CreatedAt = DateTime.UtcNow,
        };
    }
}
