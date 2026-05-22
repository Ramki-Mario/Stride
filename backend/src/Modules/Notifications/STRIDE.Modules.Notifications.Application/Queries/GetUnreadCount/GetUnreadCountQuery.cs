using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Notifications.Application.Queries.GetUnreadCount;

/// <summary>Returns the count of unread notifications for the requesting user.</summary>
public sealed record GetUnreadCountQuery(
    Guid TenantId,
    Guid RecipientId) : IRequest<Result<int>>;
