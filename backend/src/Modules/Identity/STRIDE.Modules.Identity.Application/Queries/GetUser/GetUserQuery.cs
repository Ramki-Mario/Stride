using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Identity.Application.Queries.GetUser;

/// <summary>
/// Returns the profile of a single user within the current tenant.
/// Requires an authenticated request — <see cref="TenantMiddleware"/> must
/// have set the tenant context from the JWT before this query is dispatched.
/// </summary>
public sealed record GetUserQuery(Guid UserId) : IRequest<Result<UserDto>>;
