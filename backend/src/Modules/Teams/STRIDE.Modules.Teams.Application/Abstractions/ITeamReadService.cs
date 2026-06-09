using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Teams.Application.DTOs;

namespace STRIDE.Modules.Teams.Application.Abstractions;

public interface ITeamReadService
{
    Task<PagedResult<TeamSummaryDto>> GetTeamsAsync(
        Guid tenantId, string? search, int? status,
        int page, int pageSize, CancellationToken ct = default);

    Task<TeamDetailDto?> GetTeamByIdAsync(
        Guid tenantId, Guid teamId, CancellationToken ct = default);
}
