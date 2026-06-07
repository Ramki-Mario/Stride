using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Clients.Application.Commands.DeactivateClient;

public sealed record DeactivateClientCommand(
    Guid TenantId,
    Guid ClientId,
    Guid DeactivatedBy) : IRequest<Result<Unit>>;
