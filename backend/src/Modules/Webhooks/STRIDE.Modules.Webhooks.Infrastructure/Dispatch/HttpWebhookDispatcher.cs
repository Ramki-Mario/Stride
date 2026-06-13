using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using STRIDE.Modules.Webhooks.Application.Abstractions;
using STRIDE.Modules.Webhooks.Domain.Entities;
using STRIDE.Modules.Webhooks.Domain.Repositories;

namespace STRIDE.Modules.Webhooks.Infrastructure.Dispatch;

/// <summary>
/// Fans out a signed webhook payload to every active subscription that matches the
/// given tenant and event type. Each delivery is recorded as a <see cref="WebhookDelivery"/>
/// entity — successful ones saved immediately, failed ones queued for retry by
/// <c>WebhookRetryService</c>.
///
/// Non-2xx and timeout responses increment the attempt counter via
/// <see cref="WebhookDelivery.RecordFailure"/>. When all 4 attempts are exhausted the
/// entity raises a domain event that triggers an admin notification.
/// </summary>
internal sealed class HttpWebhookDispatcher : IWebhookDispatcher
{
    internal const string HttpClientName = "WebhookDispatch";

    private const string SignatureHeader = "X-STRIDE-Signature";
    private const string EventHeader     = "X-STRIDE-Event";
    private const string DeliveryHeader  = "X-STRIDE-Delivery";

    private readonly IWebhookSubscriptionRepository _subscriptions;
    private readonly IWebhookDeliveryRepository     _deliveries;
    private readonly IHttpClientFactory             _httpClientFactory;
    private readonly ILogger<HttpWebhookDispatcher> _logger;

    public HttpWebhookDispatcher(
        IWebhookSubscriptionRepository subscriptions,
        IWebhookDeliveryRepository     deliveries,
        IHttpClientFactory             httpClientFactory,
        ILogger<HttpWebhookDispatcher> logger)
    {
        _subscriptions     = subscriptions;
        _deliveries        = deliveries;
        _httpClientFactory = httpClientFactory;
        _logger            = logger;
    }

    public async Task DispatchAsync(
        Guid tenantId, string eventType, object data, CancellationToken cancellationToken = default)
    {
        var activeSubscriptions =
            await _subscriptions.GetActiveByEventTypeAsync(tenantId, eventType, cancellationToken);

        if (activeSubscriptions.Count == 0)
            return;

        _logger.LogInformation(
            "Dispatching webhook event {EventType} to {Count} endpoint(s) for tenant {TenantId}",
            eventType, activeSubscriptions.Count, tenantId);

        foreach (var sub in activeSubscriptions)
        {
            var delivery = WebhookDelivery.Create(tenantId, sub.Id, eventType, sub.Url,
                BuildPayload(Guid.NewGuid(), eventType, tenantId, data));

            await _deliveries.AddAsync(delivery, cancellationToken);
            await _deliveries.SaveChangesAsync(cancellationToken);

            await AttemptDeliveryAsync(delivery, sub.SigningSecret, cancellationToken);
        }
    }

    internal async Task AttemptDeliveryAsync(
        WebhookDelivery delivery, string signingSecret, CancellationToken cancellationToken)
    {
        var signature = ComputeSignature(delivery.Payload, signingSecret);
        var sw        = Stopwatch.StartNew();

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);

            using var request = new HttpRequestMessage(HttpMethod.Post, delivery.Url)
            {
                Content = new StringContent(delivery.Payload, Encoding.UTF8, "application/json"),
            };
            request.Headers.TryAddWithoutValidation(SignatureHeader, $"sha256={signature}");
            request.Headers.TryAddWithoutValidation(EventHeader, delivery.EventType);
            request.Headers.TryAddWithoutValidation(DeliveryHeader, delivery.Id.ToString());

            using var response = await client.SendAsync(request, cancellationToken);
            sw.Stop();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                delivery.RecordSuccess((int)response.StatusCode, responseBody);
                _logger.LogInformation(
                    "Webhook delivery {DeliveryId} to {Url} succeeded in {ElapsedMs}ms (HTTP {StatusCode})",
                    delivery.Id, delivery.Url, sw.ElapsedMilliseconds, (int)response.StatusCode);
            }
            else
            {
                var exhausted = delivery.RecordFailure((int)response.StatusCode, responseBody);
                _logger.LogWarning(
                    "Webhook delivery {DeliveryId} to {Url} received {StatusCode} in {ElapsedMs}ms (attempt {Attempt}){Exhausted}",
                    delivery.Id, delivery.Url, (int)response.StatusCode, sw.ElapsedMilliseconds,
                    delivery.AttemptCount, exhausted ? " — EXHAUSTED" : string.Empty);
            }
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            sw.Stop();
            var exhausted = delivery.RecordFailure(null, "Request timed out.");
            _logger.LogWarning(
                "Webhook delivery {DeliveryId} to {Url} timed out after {ElapsedMs}ms (attempt {Attempt}){Exhausted}",
                delivery.Id, delivery.Url, sw.ElapsedMilliseconds, delivery.AttemptCount,
                exhausted ? " — EXHAUSTED" : string.Empty);
        }
        catch (Exception ex)
        {
            sw.Stop();
            var exhausted = delivery.RecordFailure(null, ex.Message);
            _logger.LogError(ex,
                "Webhook delivery {DeliveryId} to {Url} failed after {ElapsedMs}ms (attempt {Attempt}){Exhausted}",
                delivery.Id, delivery.Url, sw.ElapsedMilliseconds, delivery.AttemptCount,
                exhausted ? " — EXHAUSTED" : string.Empty);
        }

        _deliveries.Update(delivery);
        await _deliveries.SaveChangesAsync(cancellationToken);
    }

    internal static string BuildPayload(Guid deliveryId, string eventType, Guid tenantId, object data)
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

    internal static string ComputeSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
