using Dapper;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.BuildingBlocks.Infrastructure.Persistence;
using STRIDE.Modules.Clients.Application.Abstractions;
using STRIDE.Modules.Clients.Application.DTOs;

namespace STRIDE.Modules.Clients.Infrastructure.ReadModels;

internal sealed class ClientReadService : IClientReadService
{
    private static readonly string SqlGetClients =
        SqlLoader.Load(typeof(ClientReadService).Assembly,
            "STRIDE.Modules.Clients.Infrastructure.ReadModels.Queries.GetClients.sql");

    private static readonly string SqlCountClients =
        SqlLoader.Load(typeof(ClientReadService).Assembly,
            "STRIDE.Modules.Clients.Infrastructure.ReadModels.Queries.CountClients.sql");

    private static readonly string SqlGetById =
        SqlLoader.Load(typeof(ClientReadService).Assembly,
            "STRIDE.Modules.Clients.Infrastructure.ReadModels.Queries.GetClientById.sql");

    private static readonly string[] StatusLabels = ["Active", "Inactive"];

    private readonly IDbConnectionFactory _db;

    public ClientReadService(IDbConnectionFactory db) => _db = db;

    public async Task<PagedResult<ClientSummaryDto>> GetClientsAsync(
        Guid tenantId, string? search, int? status,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var param = new
        {
            TenantId = tenantId,
            Search   = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
            Status   = status,
            Offset   = (page - 1) * pageSize,
            PageSize = pageSize,
        };

        await using var conn = await _db.OpenConnectionAsync(cancellationToken);

        var total = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(SqlCountClients, param, cancellationToken: cancellationToken));

        if (total == 0)
            return PagedResult<ClientSummaryDto>.Empty(page, pageSize);

        var rows = await conn.QueryAsync<dynamic>(
            new CommandDefinition(SqlGetClients, param, cancellationToken: cancellationToken));

        var items = rows.Select(r => new ClientSummaryDto(
            Id:            (Guid)r.Id,
            Name:          (string)r.Name,
            ContactPerson: r.ContactPerson is DBNull ? null : (string?)r.ContactPerson,
            Email:         r.Email is DBNull ? null : (string?)r.Email,
            Phone:         r.Phone is DBNull ? null : (string?)r.Phone,
            Status:        (int)r.Status,
            StatusLabel:   StatusLabels[(int)r.Status],
            CreatedAt:     (DateTime)r.CreatedAt
        )).ToList();

        return new PagedResult<ClientSummaryDto>(items, total, page, pageSize);
    }

    public async Task<ClientDetailDto?> GetClientByIdAsync(
        Guid tenantId, Guid clientId, CancellationToken cancellationToken = default)
    {
        var param = new { TenantId = tenantId, ClientId = clientId };

        await using var conn = await _db.OpenConnectionAsync(cancellationToken);

        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            new CommandDefinition(SqlGetById, param, cancellationToken: cancellationToken));

        if (row is null) return null;

        int statusInt = (int)row.Status;

        return new ClientDetailDto(
            Id:            (Guid)row.Id,
            Name:          (string)row.Name,
            ContactPerson: row.ContactPerson is DBNull ? null : (string?)row.ContactPerson,
            Email:         row.Email is DBNull ? null : (string?)row.Email,
            Phone:         row.Phone is DBNull ? null : (string?)row.Phone,
            Address:       row.Address is DBNull ? null : (string?)row.Address,
            Notes:         row.Notes is DBNull ? null : (string?)row.Notes,
            Status:        statusInt,
            StatusLabel:   StatusLabels[statusInt],
            CreatedAt:     (DateTime)row.CreatedAt,
            UpdatedAt:     (DateTime)row.UpdatedAt);
    }
}
