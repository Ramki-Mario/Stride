using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Identity.Application.Queries.GetRole;

namespace STRIDE.Modules.Identity.Application.Queries.ListPermissions;

public sealed record ListPermissionsQuery : IRequest<Result<IReadOnlyList<PermissionDto>>>;
