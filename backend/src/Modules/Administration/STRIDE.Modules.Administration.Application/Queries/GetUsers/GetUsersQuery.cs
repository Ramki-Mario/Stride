using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Administration.Application.Queries.GetUsers;

/// <summary>
/// Returns a paged, filtered list of users for the calling tenant's administration panel.
/// </summary>
public sealed record GetUsersQuery(
    Guid TenantId,
    string? Search,
    string? Role,
    string? Status,
    int Page,
    int PageSize) : IRequest<Result<PagedResult<AdminUserDto>>>;
