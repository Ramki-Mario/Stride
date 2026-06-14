using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Identity.Application.Queries.GetUserRoles;

/// <summary>Returns the active custom roles currently assigned to a user.</summary>
public sealed record GetUserRolesQuery(Guid UserId) : IRequest<Result<IReadOnlyList<UserRoleAssignmentDto>>>;
