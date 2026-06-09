using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Notifications.Application.Commands.Push;

/// <summary>Registers a Web Push subscription endpoint for the current user.</summary>
public sealed record RegisterPushSubscriptionCommand(
    Guid TenantId,
    Guid UserId,
    string Endpoint,
    string P256dh,
    string Auth) : IRequest<Result>;
