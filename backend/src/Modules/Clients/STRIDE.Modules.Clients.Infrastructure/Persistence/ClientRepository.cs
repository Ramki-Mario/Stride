using Microsoft.EntityFrameworkCore;
using STRIDE.Modules.Clients.Domain.Entities;
using STRIDE.Modules.Clients.Domain.Repositories;

namespace STRIDE.Modules.Clients.Infrastructure.Persistence;

public sealed class ClientRepository : IClientRepository
{
    private readonly ClientsDbContext _ctx;

    public ClientRepository(ClientsDbContext ctx) => _ctx = ctx;

    public Task<Client?> GetByIdAsync(Guid tenantId, Guid clientId, CancellationToken cancellationToken = default) =>
        _ctx.Clients.FirstOrDefaultAsync(
            c => c.TenantId == tenantId && c.Id == clientId && !c.IsDeleted, cancellationToken);

    public async Task<bool> ExistsByNameAsync(
        Guid tenantId, string name, Guid? excludeId, CancellationToken cancellationToken = default)
    {
        var query = _ctx.Clients
            .Where(c => c.TenantId == tenantId && !c.IsDeleted &&
                        c.Name.ToLower() == name.ToLower().Trim());

        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Client client, CancellationToken cancellationToken = default) =>
        await _ctx.Clients.AddAsync(client, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _ctx.SaveChangesAsync(cancellationToken);
}
