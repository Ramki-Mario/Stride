using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using STRIDE.Modules.Webhooks.Application.Abstractions;
using STRIDE.Modules.Webhooks.Domain.Repositories;

namespace STRIDE.Modules.Webhooks.Infrastructure.Dispatch;

/// <summary>
/// Fans out a signed webhook payload to every active subscription that matches the
/// given tenant and event type. Uses the same HMAC-SHA256 scheme as
/// <see cref="HttpWebhookTester"/>: the hex digest of the raw JSON body, keyed by the
/// subscription's plaintext signing secret (decrypted by the EF value converter), sent
/// as <c>X-STRIDE-Signature: sha256=&lt;hex&gt;</c>.
///
/// Each delivery is attempted independently; a failure for one endpoint never prevents
/// delivery to others. All failures are logged but never re-thrown — the domain event
/// handler must not fail because of a downstream endpoint. Retry logic lives in US-182.
/// </summary>
internal sealed class HttpWebhookDispatcher : IWebhookDispatcher
{
    private const string SignatureHeader = "X-STRIDE-Signature";
    private const string EventHeader     = "X-STRIDE-Event";
    private const string DeliveryHeader  = "X-STRIDE-Delivery";

    private readonly IWebhookSubscriptionRepository        _repository;
    private readonly IHttpClientFactory                    _httpClientFactory;
    private readonly ILogger<HttpWebhookDispatcher>        _logger;

    public HttpWebhookDispatcher(
        IWebhookSubscriptionRepository        repository,
        IHttpClientFactory                    httpClientFactory,
        ILogger<HttpWebhookDispatcher>        logger)
    {
        _repository        = repository;
        _httpClientFactory = httpClientFactory;
        _logger            = logger;
    }

    public async Task DispatchAsync(
        Guid tenantId, string eventType, object data, CancellationToken cancellationToken = default)
    {
        var subscriptions = await _repository.GetActiveByEventTypeAsync(tenantId, eventType, cancellationToken);

        if (subscriptions.Count == 0)
            return;

        var deliveryId = Guid.NewGuid();
        var payload    = BuildPayload(deliveryId, eventType, tenantId, data);

        _logger.LogInformation(
            "Dispatching webhook event {EventType} (delivery {DeliveryId}) to {Count} endpoint(s) for tenant {TenantId}",
            eventType, deliveryId, subscriptions.Count, tenantId);

        var tasks = subscriptions.Select(sub =>
            DeliverAsync(sub.Url, sub.SigningSecret, eventType, deliveryId, payload, cancellationToken));

        await Task.WhenAll(tasks);
    }

    private async Task DeliverAsync(
        string url, string signingSecret, string eventType,
        Guid deliveryId, string payload, CancellationToken cancellationToken)
    {
        var signature = ComputeSignature(payload, signingSecret);
        var sw        = Stopwatch.StartNew();

        try
        {
            var client = _httpClientFactory.CreateClient(HttpWebhookTester.HttpClientName);

            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            };
            request.Headers.TryAddWithoutValidation(SignatureHeader, $"sha256={signature}");
            request.Headers.TryAddWithoutValidation(EventHeader, eventType);
            request.Headers.TryAddWithoutValidation(DeliveryHeader, deliveryId.ToString());

            using var response = await client.SendAsync(request, cancellationToken);
            sw.Stop();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Webhook delivery {DeliveryId} to {Url} succeeded in {ElapsedMs}ms (HTTP {StatusCode})",
                    deliveryId, url, sw.ElapsedMilliseconds, (int)response.StatusCode);
            }
            else
            {
                _logger.LogWarning(
                    "Webhook delivery {DeliveryId} to {Url} received non-success response {StatusCode} in {ElapsedMs}ms",
                    deliveryId, url, (int)response.StatusCode, sw.ElapsedMilliseconds);
            }
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            sw.Stop();
            _logger.LogWarning(
                "Webhook delivery {DeliveryId} to {Url} timed out after {ElapsedMs}ms",
                deliveryId, url, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "Webhook delivery {DeliveryId} to {Url} failed after {ElapsedMs}ms",
                deliveryId, url, sw.ElapsedMilliseconds);
        }
    }

    private static string BuildPayload(Guid deliveryId, string eventType, Guid tenantId, object data)
    {
        var envelope = new
        {
            id        = deliveryId,
            @event    = eventType,
            tenantId  = tenantId,
            timestamp = DateTime.UtcNow,
            data,
        };

        return JsonSerializer.Serialize(envelope, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
    }

    private static string ComputeSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
