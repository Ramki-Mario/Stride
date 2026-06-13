using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using STRIDE.Modules.Webhooks.Application.Abstractions;

namespace STRIDE.Modules.Webhooks.Infrastructure.Dispatch;

/// <summary>
/// Fires a signed sample <c>ping</c> event at a webhook URL using a named HttpClient.
/// Uses the same HMAC-SHA256 signing scheme that live deliveries (US-181) will use:
/// the hex digest of the raw JSON body keyed by the subscription's signing secret,
/// sent as <c>X-STRIDE-Signature: sha256=&lt;hex&gt;</c>.
/// </summary>
internal sealed class HttpWebhookTester : IWebhookTester
{
    public const string HttpClientName  = "WebhookDispatch";
    private const string SignatureHeader = "X-STRIDE-Signature";
    private const string EventHeader      = "X-STRIDE-Event";

    private readonly IHttpClientFactory        _httpClientFactory;
    private readonly ILogger<HttpWebhookTester> _logger;

    public HttpWebhookTester(IHttpClientFactory httpClientFactory, ILogger<HttpWebhookTester> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger            = logger;
    }

    public async Task<WebhookTestResult> SendPingAsync(
        string url, string signingSecret, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(new
        {
            @event    = "ping",
            sentAt    = DateTime.UtcNow,
            data      = new { message = "This is a test event from STRIDE." },
        });

        var signature = ComputeSignature(payload, signingSecret);
        var sw        = Stopwatch.StartNew();

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            };
            request.Headers.TryAddWithoutValidation(SignatureHeader, $"sha256={signature}");
            request.Headers.TryAddWithoutValidation(EventHeader, "ping");

            using var response = await client.SendAsync(request, cancellationToken);
            sw.Stop();

            return new WebhookTestResult(
                Success:    response.IsSuccessStatusCode,
                StatusCode: (int)response.StatusCode,
                Error:      response.IsSuccessStatusCode ? null : $"Endpoint returned {(int)response.StatusCode}.",
                ElapsedMs:  sw.ElapsedMilliseconds);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            sw.Stop();
            return new WebhookTestResult(false, null, "Request timed out.", sw.ElapsedMilliseconds);
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            _logger.LogInformation(ex, "Webhook test ping to {Url} failed", url);
            return new WebhookTestResult(false, null, "Could not connect to the endpoint.", sw.ElapsedMilliseconds);
        }
    }

    private static string ComputeSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
