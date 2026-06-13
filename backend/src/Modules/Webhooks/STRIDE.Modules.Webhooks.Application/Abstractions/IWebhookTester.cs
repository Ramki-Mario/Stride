namespace STRIDE.Modules.Webhooks.Application.Abstractions;

/// <summary>Outcome of firing a sample <c>ping</c> event at a registered webhook URL.</summary>
public sealed record WebhookTestResult(bool Success, int? StatusCode, string? Error, long ElapsedMs);

/// <summary>
/// Fires a signed sample <c>ping</c> payload at a subscription's URL so the tenant
/// admin can confirm connectivity before relying on live events. Implemented in
/// Infrastructure using <c>IHttpClientFactory</c> with the same HMAC-SHA256 signing
/// scheme real deliveries will use.
/// </summary>
public interface IWebhookTester
{
    Task<WebhookTestResult> SendPingAsync(
        string url, string signingSecret, CancellationToken cancellationToken = default);
}
