using Microsoft.AspNetCore.Authorization;
using STRIDE.BFF.Extensions;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BFF.HttpClients;

namespace STRIDE.BFF.Controllers;

/// <summary>
/// BFF proxy for Webhooks module endpoints.
///
///   GET    /bff/webhooks/event-types
///   GET    /bff/webhooks/subscriptions
///   POST   /bff/webhooks/subscriptions
///   PUT    /bff/webhooks/subscriptions/{id}
///   DELETE /bff/webhooks/subscriptions/{id}
///   POST   /bff/webhooks/subscriptions/{id}/test
///   POST   /bff/webhooks/subscriptions/{id}/regenerate-secret
///   GET    /bff/webhooks/subscriptions/{id}/deliveries
///   POST   /bff/webhooks/subscriptions/{id}/deliveries/{deliveryId}/retry
/// </summary>
[ApiController]
[Authorize]
[Route("bff/webhooks")]
public sealed class WebhooksController : ControllerBase
{
    private readonly WebhooksApiClient           _webhooks;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(WebhooksApiClient webhooks, ILogger<WebhooksController> logger)
    {
        _webhooks = webhooks;
        _logger   = logger;
    }

    [HttpGet("event-types")]
    public async Task<IActionResult> GetEventTypes(CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(await _webhooks.GetEventTypesAsync(token, cancellationToken), cancellationToken);
    }

    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetSubscriptions(CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(await _webhooks.GetSubscriptionsAsync(token, cancellationToken), cancellationToken);
    }

    [HttpPost("subscriptions")]
    public async Task<IActionResult> CreateSubscription([FromBody] object body, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _webhooks.CreateSubscriptionAsync(body, token, cancellationToken);
        return response.IsSuccessStatusCode
            ? StatusCode(StatusCodes.Status201Created, await response.Content.ReadAsStringAsync(cancellationToken))
            : await ProxyAsync(response, cancellationToken);
    }

    [HttpPut("subscriptions/{id:guid}")]
    public async Task<IActionResult> UpdateSubscription(Guid id, [FromBody] object body, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _webhooks.UpdateSubscriptionAsync(id, body, token, cancellationToken);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response, cancellationToken);
    }

    [HttpDelete("subscriptions/{id:guid}")]
    public async Task<IActionResult> DeleteSubscription(Guid id, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _webhooks.DeleteSubscriptionAsync(id, token, cancellationToken);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response, cancellationToken);
    }

    [HttpPost("subscriptions/{id:guid}/test")]
    public async Task<IActionResult> TestSubscription(Guid id, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(await _webhooks.TestSubscriptionAsync(id, token, cancellationToken), cancellationToken);
    }

    [HttpPost("subscriptions/{id:guid}/regenerate-secret")]
    public async Task<IActionResult> RegenerateSecret(Guid id, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(await _webhooks.RegenerateSecretAsync(id, token, cancellationToken), cancellationToken);
    }

    [HttpGet("subscriptions/{id:guid}/deliveries")]
    public async Task<IActionResult> GetDeliveries(
        Guid id, [FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(await _webhooks.GetDeliveriesAsync(id, limit, token, cancellationToken), cancellationToken);
    }

    [HttpPost("subscriptions/{id:guid}/deliveries/{deliveryId:guid}/retry")]
    public async Task<IActionResult> RetryDelivery(Guid id, Guid deliveryId, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _webhooks.RetryDeliveryAsync(id, deliveryId, token, cancellationToken);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response, cancellationToken);
    }

    // â”€â”€ Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private Task<string?> GetTokenAsync() => HttpContext.GetCurrentAccessTokenAsync();

    private async Task<IActionResult> ProxyAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Host webhooks API returned {StatusCode}: {Body}", (int)response.StatusCode, json);
            return StatusCode((int)response.StatusCode, new ProblemDetails
            {
                Title  = "Upstream error",
                Detail = json,
                Status = (int)response.StatusCode,
            });
        }

        return new ContentResult
        {
            Content     = json,
            ContentType = "application/json",
            StatusCode  = StatusCodes.Status200OK,
        };
    }
}

