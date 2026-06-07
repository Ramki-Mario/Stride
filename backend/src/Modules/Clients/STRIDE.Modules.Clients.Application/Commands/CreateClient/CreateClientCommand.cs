using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Clients.Application.Commands.CreateClient;

public sealed record CreateClientCommand(
    Guid    TenantId,
    string  Name,
    string? ContactPerson,
    string? Email,
    string? Phone,
    string? Address,
    string? Notes,
    Guid    CreatedBy) : IRequest<Result<Guid>>;
