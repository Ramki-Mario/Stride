using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Clients.Application.Commands.UpdateClient;

public sealed record UpdateClientCommand(
    Guid    TenantId,
    Guid    ClientId,
    string  Name,
    string? ContactPerson,
    string? Email,
    string? Phone,
    string? Address,
    string? Notes,
    Guid    UpdatedBy) : IRequest<Result<Unit>>;
