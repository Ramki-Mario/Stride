using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BFF.HttpClients;

namespace STRIDE.BFF.Controllers;

/// <summary>
/// BFF proxy for Notifications module endpoints.
///
/// Angular calls /bff/notifications/* — this controller forwards with the session
/// JWT so STRIDE.Host can authenticate and apply tenant isolation.
///
///   GET    /bff/notifications              — list notifications for current user
///   GET    /bff/notifications/unread-count — unread badge count
///   POST   /bff/notifications/{id}/read   — mark notification as read
///   DELETE /bff/notifications/{id}        — soft-delete notification
/// </summary>
[ApiController]
[Authorize]
[Route("bff/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly NotificationsApiClient            _notifications;
    private readonly ILogger<NotificationsController>  _logger;

    public NotificationsController(
        NotificationsApiClient notifications,
        ILogger<NotificationsController> logger)
    {
        _notifications = notifications;
        _logger        = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications(CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(await _notifications.GetNotificationsAsync(token, cancellationToken), cancellationToken);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(await _notifications.GetUnreadCountAsync(token, cancellationToken), cancellationToken);
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _notifications.MarkAsReadAsync(id, token, cancellationToken);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response, cancellationToken);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteNotification(Guid id, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _notifications.DeleteNotificationAsync(id, token, cancellationToken);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response, cancellationToken);
    }

    // ── Web Push ──────────────────────────────────────────────────────────

    /// <summary>
    /// Proxies the VAPID public key — no auth required so the Angular app can
    /// call this before the user is fully authenticated.
    /// GET /bff/notifications/push/vapid-key
    /// </summary>
    [HttpGet("push/vapid-key")]
    [AllowAnonymous]
    public async Task<IActionResult> GetVapidPublicKey(CancellationToken cancellationToken)
        => await ProxyAsync(await _notifications.GetVapidPublicKeyAsync(cancellationToken), cancellationToken);

    /// <summary>
    /// Registers a Web Push subscription for the current session user.
    /// POST /bff/notifications/push/subscribe
    /// </summary>
    [HttpPost("push/subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] object body, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var json = System.Text.Json.JsonSerializer.Serialize(body);
        var response = await _notifications.SubscribePushAsync(token, json, cancellationToken);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response, cancellationToken);
    }

    /// <summary>
    /// Removes a Web Push subscription.
    /// POST /bff/notifications/push/unsubscribe
    /// </summary>
    [HttpPost("push/unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] object body, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var json = System.Text.Json.JsonSerializer.Serialize(body);
        var response = await _notifications.UnsubscribePushAsync(token, json, cancellationToken);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response, cancellationToken);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private Task<string?> GetTokenAsync() => HttpContext.GetTokenAsync("access_token");

    private async Task<IActionResult> ProxyAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Host notifications API returned {StatusCode}: {Body}",
                (int)response.StatusCode, json);

            return StatusCode((int)response.StatusCode,
                new ProblemDetails
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
