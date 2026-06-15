using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Identity.Application.Queries.GetRole;

namespace STRIDE.Modules.Identity.Application.Queries.GetMyRoles;

/// <summary>
/// Returns the roles assigned to the currently authenticated user, including each
/// role's full permission set. Drives the "My Access" section on the profile page.
/// </summary>
public sealed record GetMyRolesQuery() : IRequest<Result<IReadOnlyList<RoleDetailDto>>>;
