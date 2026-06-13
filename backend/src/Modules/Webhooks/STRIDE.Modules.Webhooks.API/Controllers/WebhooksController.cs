using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Webhooks.API.DTOs;
using STRIDE.Modules.Webhooks.Application.Commands.CreateWebhookSubscription;
using STRIDE.Modules.Webhooks.Application.Commands.DeleteWebhookSubscription;
using STRIDE.Modules.Webhooks.Application.Commands.RegenerateWebhookSecret;
using STRIDE.Modules.Webhooks.Application.Commands.TestWebhookSubscription;
using STRIDE.Modules.Webhooks.Application.Commands.UpdateWebhookSubscription;
using STRIDE.Modules.Webhooks.Application.Queries.GetWebhookEventTypes;
using STRIDE.Modules.Webhooks.Application.Queries.GetWebhookSubscriptions;

namespace STRIDE.Modules.Webhooks.API.Controllers;

/// <summary>
/// Tenant webhook subscription management (admin only — maps to the tenant.settings capability).
///
///   GET    /api/webhooks/event-types                       — subscribable event catalog
///   GET    /api/webhooks/subscriptions                     — list subscriptions (no secrets)
///   POST   /api/webhooks/subscriptions                     — create (returns signing secret once)
///   PUT    /api/webhooks/subscriptions/{id}                — update
///   DELETE /api/webhooks/subscriptions/{id}                — soft-delete
///   POST   /api/webhooks/subscriptions/{id}/test           — fire a sample ping
///   POST   /api/webhooks/subscriptions/{id}/regenerate-secret — rotate the signing secret
/// </summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/webhooks")]
public sealed class WebhooksController : ControllerBase
{
    private readonly IMediator      _mediator;
    private readonly ICurrentUser   _currentUser;
    private readonly ITenantContext _tenantContext;

    public WebhooksController(IMediator mediator, ICurrentUser currentUser, ITenantContext tenantContext)
    {
        _mediator      = mediator;
        _currentUser   = currentUser;
        _tenantContext = tenantContext;
    }

    [HttpGet("event-types")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEventTypes(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetWebhookEventTypesQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("subscriptions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubscriptions(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetWebhookSubscriptionsQuery(_tenantContext.TenantId), cancellationToken);
        return Ok(result.Value);
    }

    [HttpPost("subscriptions")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSubscription(
        [FromBody] CreateWebhookSubscriptionRequest body, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateWebhookSubscriptionCommand(
            _tenantContext.TenantId, body.Url, body.EventTypes ?? Array.Empty<string>(),
            _currentUser.UserId), cancellationToken);

        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : BadRequest(new ProblemDetails { Title = "Could not create webhook", Detail = result.Error });
    }

    [HttpPut("subscriptions/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSubscription(
        Guid id, [FromBody] UpdateWebhookSubscriptionRequest body, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateWebhookSubscriptionCommand(
            _tenantContext.TenantId, id, body.Url, body.EventTypes ?? Array.Empty<string>(),
            body.IsActive, _currentUser.UserId), cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : BadRequest(new ProblemDetails { Title = "Could not update webhook", Detail = result.Error });
    }

    [HttpDelete("subscriptions/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteSubscription(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new DeleteWebhookSubscriptionCommand(_tenantContext.TenantId, id, _currentUser.UserId),
            cancellationToken);

        return result.IsSuccess
            ? NoContent()
            : BadRequest(new ProblemDetails { Title = "Could not delete webhook", Detail = result.Error });
    }

    [HttpPost("subscriptions/{id:guid}/test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TestSubscription(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new TestWebhookSubscriptionCommand(_tenantContext.TenantId, id), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new ProblemDetails { Title = "Could not test webhook", Detail = result.Error });
    }

    [HttpPost("subscriptions/{id:guid}/regenerate-secret")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegenerateSecret(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RegenerateWebhookSecretCommand(_tenantContext.TenantId, id, _currentUser.UserId),
            cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new ProblemDetails { Title = "Could not regenerate secret", Detail = result.Error });
    }
}
