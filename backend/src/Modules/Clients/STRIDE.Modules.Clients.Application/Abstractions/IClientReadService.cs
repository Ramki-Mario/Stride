using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Clients.Application.DTOs;

namespace STRIDE.Modules.Clients.Application.Abstractions;

public interface IClientReadService
{
    Task<PagedResult<ClientSummaryDto>> GetClientsAsync(
        Guid    tenantId,
        string? search,
        int?    status,
        int     page,
        int     pageSize,
        CancellationToken cancellationToken = default);

    Task<ClientDetailDto?> GetClientByIdAsync(
        Guid tenantId,
        Guid clientId,
        CancellationToken cancellationToken = default);

    Task<ClientHistoryDto?> GetClientHistoryAsync(
        Guid tenantId,
        Guid clientId,
        CancellationToken cancellationToken = default);
}
