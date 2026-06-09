using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Notifications.Application.Queries.GetVapidPublicKey;

/// <summary>Returns the VAPID public key so the frontend can subscribe to Web Push.</summary>
public sealed record GetVapidPublicKeyQuery : IRequest<Result<string>>;
