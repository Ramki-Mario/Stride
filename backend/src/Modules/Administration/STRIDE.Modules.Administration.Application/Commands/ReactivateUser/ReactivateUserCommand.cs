using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Administration.Application.Commands.ReactivateUser;

public sealed record ReactivateUserCommand(
    Guid TenantId,
    Guid UserId) : IRequest<Result>;
