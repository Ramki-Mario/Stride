using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Identity.Application.Commands.DeleteRole;

public sealed record DeleteRoleCommand(Guid RoleId, Guid ActorId) : IRequest<Result>;
