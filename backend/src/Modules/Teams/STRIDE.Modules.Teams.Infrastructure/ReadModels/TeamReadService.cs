using Dapper;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.BuildingBlocks.Infrastructure.Persistence;
using STRIDE.Modules.Teams.Application.Abstractions;
using STRIDE.Modules.Teams.Application.DTOs;

namespace STRIDE.Modules.Teams.Infrastructure.ReadModels;

internal sealed class TeamReadService : ITeamReadService
{
    private static readonly string SqlGetTeams =
        SqlLoader.Load(typeof(TeamReadService).Assembly,
            "STRIDE.Modules.Teams.Infrastructure.ReadModels.Queries.GetTeams.sql");

    private static readonly string SqlCountTeams =
        SqlLoader.Load(typeof(TeamReadService).Assembly,
            "STRIDE.Modules.Teams.Infrastructure.ReadModels.Queries.CountTeams.sql");

    private static readonly string SqlGetById =
        SqlLoader.Load(typeof(TeamReadService).Assembly,
            "STRIDE.Modules.Teams.Infrastructure.ReadModels.Queries.GetTeamById.sql");

    private static readonly string[] StatusLabels = ["Active", "Inactive"];

    private readonly IDbConnectionFactory _db;

    public TeamReadService(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<TeamSummaryDto>> GetTeamsAsync(
        Guid tenantId, string? search, int? status,
        int page, int pageSize, CancellationToken ct = default)
    {
        var param = new
        {
            TenantId = tenantId,
            Search   = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
            Status   = status,
            Offset   = (page - 1) * pageSize,
            PageSize = pageSize,
        };

        await using var conn = await _db.OpenConnectionAsync(ct);

        var total = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(SqlCountTeams, param, cancellationToken: ct));

        if (total == 0)
            return PagedResult<TeamSummaryDto>.Empty(page, pageSize);

        var rows = await conn.QueryAsync<dynamic>(
            new CommandDefinition(SqlGetTeams, param, cancellationToken: ct));

        var items = rows.Select(r => new TeamSummaryDto(
            Id:             (Guid)r.Id,
            Name:           (string)r.Name,
            Description:    r.Description is DBNull ? null : (string?)r.Description,
            ParentTeamId:   r.ParentTeamId is DBNull ? null : (Guid?)r.ParentTeamId,
            ParentTeamName: r.ParentTeamName is DBNull ? null : (string?)r.ParentTeamName,
            Status:         (int)r.Status,
            StatusLabel:    StatusLabels[(int)r.Status],
            CreatedAt:      (DateTime)r.CreatedAt
        )).ToList();

        return new PagedResult<TeamSummaryDto>(items, total, page, pageSize);
    }

    public async Task<TeamDetailDto?> GetTeamByIdAsync(
        Guid tenantId, Guid teamId, CancellationToken ct = default)
    {
        var param = new { TenantId = tenantId, TeamId = teamId };

        await using var conn = await _db.OpenConnectionAsync(ct);

        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            new CommandDefinition(SqlGetById, param, cancellationToken: ct));

        if (row is null) return null;

        int statusInt = (int)row.Status;

        return new TeamDetailDto(
            Id:             (Guid)row.Id,
            Name:           (string)row.Name,
            Description:    row.Description is DBNull ? null : (string?)row.Description,
            ParentTeamId:   row.ParentTeamId is DBNull ? null : (Guid?)row.ParentTeamId,
            ParentTeamName: row.ParentTeamName is DBNull ? null : (string?)row.ParentTeamName,
            Status:         statusInt,
            StatusLabel:    StatusLabels[statusInt],
            CreatedAt:      (DateTime)row.CreatedAt,
            UpdatedAt:      (DateTime)row.UpdatedAt);
    }
}
