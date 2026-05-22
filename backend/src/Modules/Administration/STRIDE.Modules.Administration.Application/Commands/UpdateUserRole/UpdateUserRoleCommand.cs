using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Administration.Application.Commands.UpdateUserRole;

public sealed record UpdateUserRoleCommand(
    Guid TenantId,
    Guid UserId,
    string NewRole,
    Guid UpdatedBy) : IRequest<Result>;
