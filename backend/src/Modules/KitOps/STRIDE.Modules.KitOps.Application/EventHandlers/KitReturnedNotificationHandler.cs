using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.KitOps.Domain.Events;

namespace STRIDE.Modules.KitOps.Application.EventHandlers;

internal sealed class KitReturnedNotificationHandler
    : INotificationHandler<DomainEventNotification<KitReturnedEvent>>
{
    private readonly IEmailSender _emailSender;
    private readonly IUserRoleQueryService _userRoles;
    private readonly ILogger<KitReturnedNotificationHandler> _logger;

    public KitReturnedNotificationHandler(
        IEmailSender emailSender,
        IUserRoleQueryService userRoles,
        ILogger<KitReturnedNotificationHandler> logger)
    {
        _emailSender = emailSender;
        _userRoles   = userRoles;
        _logger      = logger;
    }

    public async Task Handle(
        DomainEventNotification<KitReturnedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        var adminEmails = await _userRoles.GetEmailsByRoleNameAsync(
            "Admin", e.TenantId, cancellationToken);

        if (adminEmails.Count == 0)
            return;

        var subject = $"Kit item returned: {e.KitItemId:N}";
        var htmlBody = $"""
            <p>A field user has returned a kit item.</p>
            <ul>
                <li><strong>Kit Item ID:</strong> {e.KitItemId:N}</li>
                <li><strong>Returned By:</strong> {e.ReturnedByUserId:N}</li>
                <li><strong>Returned At:</strong> {e.ReturnedAt:yyyy-MM-dd HH:mm} UTC</li>
            </ul>
            """;

        foreach (var email in adminEmails)
        {
            try
            {
                await _emailSender.SendAsync(email, subject, htmlBody, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to send KitReturned notification email to {Email}",
                    email);
            }
        }
    }
}
