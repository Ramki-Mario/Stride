using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Administration.Application.Commands.DeactivateUser;

public sealed record DeactivateUserCommand(
    Guid TenantId,
    Guid UserId,
    Guid CallerUserId) : IRequest<Result>;
