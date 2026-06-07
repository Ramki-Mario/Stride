using STRIDE.Modules.Clients.Domain.Entities;

namespace STRIDE.Modules.Clients.Domain.Repositories;

public interface IClientRepository
{
    Task<Client?> GetByIdAsync(Guid tenantId, Guid clientId, CancellationToken cancellationToken = default);
    Task<bool>    ExistsByNameAsync(Guid tenantId, string name, Guid? excludeId, CancellationToken cancellationToken = default);
    Task          AddAsync(Client client, CancellationToken cancellationToken = default);
    Task          SaveChangesAsync(CancellationToken cancellationToken = default);
}
