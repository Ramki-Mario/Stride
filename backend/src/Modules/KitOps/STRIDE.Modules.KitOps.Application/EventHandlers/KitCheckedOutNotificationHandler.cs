using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.KitOps.Domain.Events;

namespace STRIDE.Modules.KitOps.Application.EventHandlers;

internal sealed class KitCheckedOutNotificationHandler
    : INotificationHandler<DomainEventNotification<KitCheckedOutEvent>>
{
    private readonly IEmailSender _emailSender;
    private readonly IUserRoleQueryService _userRoles;
    private readonly ILogger<KitCheckedOutNotificationHandler> _logger;

    public KitCheckedOutNotificationHandler(
        IEmailSender emailSender,
        IUserRoleQueryService userRoles,
        ILogger<KitCheckedOutNotificationHandler> logger)
    {
        _emailSender = emailSender;
        _userRoles   = userRoles;
        _logger      = logger;
    }

    public async Task Handle(
        DomainEventNotification<KitCheckedOutEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        var adminEmails = await _userRoles.GetEmailsByRoleNameAsync(
            "Admin", e.TenantId, cancellationToken);

        if (adminEmails.Count == 0)
            return;

        var subject = $"Kit item checked out: {e.KitItemId:N}";
        var htmlBody = $"""
            <p>A field user has checked out a kit item.</p>
            <ul>
                <li><strong>Kit Item ID:</strong> {e.KitItemId:N}</li>
                <li><strong>User ID:</strong> {e.CheckedOutByUserId:N}</li>
                <li><strong>Expected Return:</strong> {e.ExpectedReturnAt:yyyy-MM-dd HH:mm} UTC</li>
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
                    "Failed to send KitCheckedOut notification email to {Email}",
                    email);
            }
        }
    }
}
