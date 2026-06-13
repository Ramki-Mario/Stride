using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Identity.Domain.Events;
using STRIDE.Modules.Webhooks.Application.Abstractions;

namespace STRIDE.Modules.Webhooks.Application.EventHandlers;

/// <summary>
/// Dispatches a <c>user.invited</c> webhook payload whenever a new user is created
/// in a tenant (whether via the onboarding flow or via the admin invite command).
/// Maps to <see cref="UserCreatedEvent"/> as there is no separate UserInvitedEvent.
/// </summary>
internal sealed class UserInvitedDispatchHandler
    : INotificationHandler<DomainEventNotification<UserCreatedEvent>>
{
    private readonly IWebhookDispatcher                   _dispatcher;
    private readonly ILogger<UserInvitedDispatchHandler>  _logger;

    public UserInvitedDispatchHandler(
        IWebhookDispatcher dispatcher,
        ILogger<UserInvitedDispatchHandler> logger)
    {
        _dispatcher = dispatcher;
        _logger     = logger;
    }

    public async Task Handle(
        DomainEventNotification<UserCreatedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        try
        {
            await _dispatcher.DispatchAsync(
                tenantId:  e.TenantId,
                eventType: Domain.WebhookEventTypes.UserInvited,
                data: new
                {
                    userId = e.UserId,
                    email  = e.Email,
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled exception dispatching webhook for user.invited (user {Id})",
                e.UserId);
        }
    }
}
