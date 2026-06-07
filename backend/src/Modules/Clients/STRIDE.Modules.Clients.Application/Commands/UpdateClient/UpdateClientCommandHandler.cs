using MediatR;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Clients.Domain.Exceptions;
using STRIDE.Modules.Clients.Domain.Repositories;

namespace STRIDE.Modules.Clients.Application.Commands.UpdateClient;

internal sealed class UpdateClientCommandHandler : IRequestHandler<UpdateClientCommand, Result<Unit>>
{
    private readonly IClientRepository _repo;
    private readonly IAuditLogger      _audit;
    private readonly ICurrentUser      _currentUser;

    public UpdateClientCommandHandler(
        IClientRepository repo,
        IAuditLogger      audit,
        ICurrentUser      currentUser)
    {
        _repo        = repo;
        _audit       = audit;
        _currentUser = currentUser;
    }

    public async Task<Result<Unit>> Handle(UpdateClientCommand request, CancellationToken cancellationToken)
    {
        var client = await _repo.GetByIdAsync(request.TenantId, request.ClientId, cancellationToken);
        if (client is null)
            return Result<Unit>.Failure("Client not found.");

        var nameExists = await _repo.ExistsByNameAsync(
            request.TenantId, request.Name, excludeId: request.ClientId, cancellationToken);

        if (nameExists)
            return Result<Unit>.Failure($"A client named '{request.Name}' already exists.");

        try
        {
            client.Update(
                request.Name,
                request.ContactPerson,
                request.Email,
                request.Phone,
                request.Address,
                request.Notes,
                request.UpdatedBy);

            await _repo.SaveChangesAsync(cancellationToken);

            await _audit.LogAsync(new AuditLogEntry(
                TenantId:     request.TenantId,
                ActorId:      request.UpdatedBy,
                ActorEmail:   _currentUser.Email,
                Action:       AuditActions.ClientUpdated,
                ResourceType: "Client",
                ResourceId:   request.ClientId,
                NewValueJson: $"{{\"name\":\"{request.Name}\"}}"));

            return Result<Unit>.Success(Unit.Value);
        }
        catch (ClientDomainException ex)
        {
            return Result<Unit>.Failure(ex.Message);
        }
    }
}
