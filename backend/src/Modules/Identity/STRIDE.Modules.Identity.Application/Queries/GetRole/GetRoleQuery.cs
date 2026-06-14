using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Identity.Application.Queries.GetRole;

public sealed record GetRoleQuery(Guid RoleId) : IRequest<Result<RoleDetailDto>>;
