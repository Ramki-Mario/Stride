using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Clients.Application.DTOs;

namespace STRIDE.Modules.Clients.Application.Queries.GetClients;

public sealed record GetClientsQuery(
    Guid    TenantId,
    string? Search,
    int?    Status,
    int     Page,
    int     PageSize) : IRequest<Result<PagedResult<ClientSummaryDto>>>;
