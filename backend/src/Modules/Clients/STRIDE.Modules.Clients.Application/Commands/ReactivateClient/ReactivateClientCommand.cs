using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Clients.Application.Commands.ReactivateClient;

public sealed record ReactivateClientCommand(
    Guid TenantId,
    Guid ClientId,
    Guid ReactivatedBy) : IRequest<Result<Unit>>;
