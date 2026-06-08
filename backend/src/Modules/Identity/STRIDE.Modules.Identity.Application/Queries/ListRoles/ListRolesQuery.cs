using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Identity.Application.Queries.ListRoles;

/// <summary>
/// Returns all active (non-deleted) roles for the current tenant.
/// Used by the workflow builder role-assignment dropdown.
/// </summary>
public sealed record ListRolesQuery : IRequest<Result<IReadOnlyList<RoleDto>>>;
