using Lib.Net.Http.WebPush;
using Lib.Net.Http.WebPush.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using STRIDE.Modules.Notifications.Application;
using DomainSubscription = STRIDE.Modules.Notifications.Domain.Entities.PushSubscription;

namespace STRIDE.Modules.Notifications.Infrastructure.Services;

/// <summary>
/// Sends Web Push notifications using VAPID authentication via Lib.Net.Http.WebPush.
/// Delivery failures are swallowed and logged so the in-app notification path is not blocked.
/// </summary>
internal sealed class WebPushService : IWebPushService
{
    private readonly PushServiceClient _client;
    private readonly ILogger<WebPushService> _logger;

    public WebPushService(IConfiguration configuration, ILogger<WebPushService> logger)
    {
        _logger = logger;

        var subject   = configuration["Vapid:Subject"]    ?? string.Empty;
        var publicKey = configuration["Vapid:PublicKey"]  ?? string.Empty;
        var privateKey = configuration["Vapid:PrivateKey"] ?? string.Empty;

        _client = new PushServiceClient();
        _client.DefaultAuthentication = new VapidAuthentication(publicKey, privateKey)
        {
            Subject = subject,
        };
    }

    public async Task SendAsync(
        DomainSubscription subscription,
        string title,
        string body,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var pushSub = new PushSubscription
            {
                Endpoint = subscription.Endpoint,
                Keys     = new Dictionary<string, string>
                {
                    ["p256dh"] = subscription.P256dh,
                    ["auth"]   = subscription.Auth,
                },
            };

            var payload = System.Text.Json.JsonSerializer.Serialize(new { title, body });
            var message = new PushMessage(payload) { TimeToLive = 86400 };

            await _client.RequestPushMessageDeliveryAsync(pushSub, message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Web Push delivery failed for subscription endpoint {Endpoint}", subscription.Endpoint);
        }
    }

}
