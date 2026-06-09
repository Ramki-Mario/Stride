using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Notifications.Application.Commands.Push;

/// <summary>Removes a Web Push subscription by endpoint URL.</summary>
public sealed record UnregisterPushSubscriptionCommand(
    Guid TenantId,
    string Endpoint) : IRequest<Result>;
