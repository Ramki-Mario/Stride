using STRIDE.Modules.Notifications.Domain.Entities;

namespace STRIDE.Modules.Notifications.Application;

/// <summary>
/// Sends Web Push notifications to a browser-registered push subscription.
/// The implementation uses Lib.Net.Http.WebPush with VAPID authentication.
/// </summary>
public interface IWebPushService
{
    /// <summary>
    /// Sends a push notification payload to the given subscription endpoint.
    /// Silently swallows delivery errors to avoid blocking the in-app notification path.
    /// </summary>
    Task SendAsync(PushSubscription subscription, string title, string body, CancellationToken cancellationToken = default);
}
