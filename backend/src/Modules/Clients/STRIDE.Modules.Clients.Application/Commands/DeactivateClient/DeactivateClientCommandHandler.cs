using MediatR;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Clients.Domain.Exceptions;
using STRIDE.Modules.Clients.Domain.Repositories;

namespace STRIDE.Modules.Clients.Application.Commands.DeactivateClient;

internal sealed class DeactivateClientCommandHandler : IRequestHandler<DeactivateClientCommand, Result<Unit>>
{
    private readonly IClientRepository _repo;
    private readonly IAuditLogger      _audit;
    private readonly ICurrentUser      _currentUser;

    public DeactivateClientCommandHandler(
        IClientRepository repo,
        IAuditLogger      audit,
        ICurrentUser      currentUser)
    {
        _repo        = repo;
        _audit       = audit;
        _currentUser = currentUser;
    }

    public async Task<Result<Unit>> Handle(DeactivateClientCommand request, CancellationToken cancellationToken)
    {
        var client = await _repo.GetByIdAsync(request.TenantId, request.ClientId, cancellationToken);
        if (client is null)
            return Result<Unit>.Failure("Client not found.");

        try
        {
            client.Deactivate(request.DeactivatedBy);
            await _repo.SaveChangesAsync(cancellationToken);

            await _audit.LogAsync(new AuditLogEntry(
                TenantId:     request.TenantId,
                ActorId:      request.DeactivatedBy,
                ActorEmail:   _currentUser.Email,
                Action:       AuditActions.ClientDeactivated,
                ResourceType: "Client",
                ResourceId:   request.ClientId,
                NewValueJson: null));

            return Result<Unit>.Success(Unit.Value);
        }
        catch (ClientDomainException ex)
        {
            return Result<Unit>.Failure(ex.Message);
        }
    }
}
