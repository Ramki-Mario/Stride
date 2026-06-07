using MediatR;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Clients.Domain.Entities;
using STRIDE.Modules.Clients.Domain.Exceptions;
using STRIDE.Modules.Clients.Domain.Repositories;

namespace STRIDE.Modules.Clients.Application.Commands.CreateClient;

internal sealed class CreateClientCommandHandler : IRequestHandler<CreateClientCommand, Result<Guid>>
{
    private readonly IClientRepository _repo;
    private readonly IAuditLogger      _audit;
    private readonly ICurrentUser      _currentUser;

    public CreateClientCommandHandler(
        IClientRepository repo,
        IAuditLogger      audit,
        ICurrentUser      currentUser)
    {
        _repo        = repo;
        _audit       = audit;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(CreateClientCommand request, CancellationToken cancellationToken)
    {
        var nameExists = await _repo.ExistsByNameAsync(
            request.TenantId, request.Name, excludeId: null, cancellationToken);

        if (nameExists)
            return Result<Guid>.Failure($"A client named '{request.Name}' already exists.");

        try
        {
            var client = Client.Create(new NewClient(
                TenantId:      request.TenantId,
                Name:          request.Name,
                ContactPerson: request.ContactPerson,
                Email:         request.Email,
                Phone:         request.Phone,
                Address:       request.Address,
                Notes:         request.Notes,
                CreatedBy:     request.CreatedBy));

            await _repo.AddAsync(client, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);

            await _audit.LogAsync(new AuditLogEntry(
                TenantId:     request.TenantId,
                ActorId:      request.CreatedBy,
                ActorEmail:   _currentUser.Email,
                Action:       AuditActions.ClientCreated,
                ResourceType: "Client",
                ResourceId:   client.Id,
                NewValueJson: $"{{\"name\":\"{request.Name}\"}}"));

            return Result<Guid>.Success(client.Id);
        }
        catch (ClientDomainException ex)
        {
            return Result<Guid>.Failure(ex.Message);
        }
    }
}
