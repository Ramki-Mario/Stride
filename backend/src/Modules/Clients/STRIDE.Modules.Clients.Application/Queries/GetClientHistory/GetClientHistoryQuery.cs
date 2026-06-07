using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Clients.Application.DTOs;

namespace STRIDE.Modules.Clients.Application.Queries.GetClientHistory;

public sealed record GetClientHistoryQuery(
    Guid TenantId,
    Guid ClientId) : IRequest<Result<ClientHistoryDto>>;
