using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Notifications.Application.Commands.Push;
using STRIDE.Modules.Notifications.Application.Queries.GetVapidPublicKey;

namespace STRIDE.Modules.Notifications.API.Controllers;

/// <summary>
/// Web Push subscription management.
///
///   GET  /api/notifications/push/vapid-key  — return the VAPID public key
///   POST /api/notifications/push/subscribe  — register a push subscription
///   POST /api/notifications/push/unsubscribe — remove a push subscription
/// </summary>
[ApiController]
[Authorize]
[Route("api/notifications/push")]
public sealed class PushController : ControllerBase
{
    private readonly IMediator      _mediator;
    private readonly ICurrentUser   _currentUser;
    private readonly ITenantContext _tenantContext;

    public PushController(
        IMediator mediator,
        ICurrentUser currentUser,
        ITenantContext tenantContext)
    {
        _mediator      = mediator;
        _currentUser   = currentUser;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Returns the VAPID public key the Angular service worker uses to subscribe.
    /// GET /api/notifications/push/vapid-key
    /// </summary>
    [HttpGet("vapid-key")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVapidPublicKey(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetVapidPublicKeyQuery(), cancellationToken);
        return result.IsFailure
            ? StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = result.Error })
            : Ok(new { publicKey = result.Value });
    }

    /// <summary>
    /// Registers a Web Push subscription for the authenticated user.
    /// POST /api/notifications/push/subscribe
    /// </summary>
    [HttpPost("subscribe")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscribeRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterPushSubscriptionCommand(
            TenantId: _tenantContext.TenantId,
            UserId:   _currentUser.UserId,
            Endpoint: request.Endpoint,
            P256dh:   request.P256dh,
            Auth:     request.Auth);

        var result = await _mediator.Send(command, cancellationToken);
        return result.IsFailure
            ? BadRequest(new { error = result.Error })
            : NoContent();
    }

    /// <summary>
    /// Removes a Web Push subscription.
    /// POST /api/notifications/push/unsubscribe
    /// </summary>
    [HttpPost("unsubscribe")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Unsubscribe([FromBody] PushUnsubscribeRequest request, CancellationToken cancellationToken)
    {
        var command = new UnregisterPushSubscriptionCommand(
            TenantId: _tenantContext.TenantId,
            Endpoint: request.Endpoint);

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}

public sealed record PushSubscribeRequest(string Endpoint, string P256dh, string Auth);
public sealed record PushUnsubscribeRequest(string Endpoint);
