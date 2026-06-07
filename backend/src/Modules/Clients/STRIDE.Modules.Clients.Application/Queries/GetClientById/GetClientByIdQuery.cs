using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Clients.Application.DTOs;

namespace STRIDE.Modules.Clients.Application.Queries.GetClientById;

public sealed record GetClientByIdQuery(
    Guid TenantId,
    Guid ClientId) : IRequest<Result<ClientDetailDto>>;
