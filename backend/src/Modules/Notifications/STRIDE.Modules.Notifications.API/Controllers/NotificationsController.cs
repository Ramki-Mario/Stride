using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Notifications.Application.Commands.DeleteNotification;
using STRIDE.Modules.Notifications.Application.Commands.MarkNotificationAsRead;
using STRIDE.Modules.Notifications.Application.Queries.GetNotifications;
using STRIDE.Modules.Notifications.Application.Queries.GetUnreadCount;

namespace STRIDE.Modules.Notifications.API.Controllers;

/// <summary>
/// Notifications for the authenticated user.
///
///   GET    /api/notifications              — list all (non-deleted), newest first
///   GET    /api/notifications/unread-count — unread badge count
///   POST   /api/notifications/{id}/read    — mark a notification as read
///   DELETE /api/notifications/{id}         — soft-delete a notification
/// </summary>
[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly IMediator      _mediator;
    private readonly ICurrentUser   _currentUser;
    private readonly ITenantContext _tenantContext;

    public NotificationsController(
        IMediator mediator,
        ICurrentUser currentUser,
        ITenantContext tenantContext)
    {
        _mediator      = mediator;
        _currentUser   = currentUser;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Returns all non-deleted notifications for the current user, newest first.
    /// GET /api/notifications
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotifications(CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetNotificationsQuery(_tenantContext.TenantId, _currentUser.UserId), ct);

        return Ok(result.Value);
    }

    /// <summary>
    /// Returns the count of unread notifications for the current user.
    /// GET /api/notifications/unread-count
    /// </summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
    {
        var result = await _mediator.Send(
            new GetUnreadCountQuery(_tenantContext.TenantId, _currentUser.UserId), ct);

        return Ok(result.Value);
    }

    /// <summary>
    /// Marks a notification as read. Idempotent.
    /// POST /api/notifications/{id}/read
    /// </summary>
    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new MarkNotificationAsReadCommand(_tenantContext.TenantId, id, _currentUser.UserId), ct);

        if (result.IsFailure)
        {
            if (result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { error = result.Error });

            if (result.Error.Contains("permission", StringComparison.OrdinalIgnoreCase))
                return Forbid();

            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }

    /// <summary>
    /// Soft-deletes a notification for the current user.
    /// DELETE /api/notifications/{id}
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteNotification(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new DeleteNotificationCommand(_tenantContext.TenantId, id, _currentUser.UserId), ct);

        if (result.IsFailure)
        {
            if (result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { error = result.Error });

            if (result.Error.Contains("permission", StringComparison.OrdinalIgnoreCase))
                return Forbid();

            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }
}
