using Microsoft.EntityFrameworkCore;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Infrastructure.Persistence;
using STRIDE.Modules.Identity.Application.Abstractions;
using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Infrastructure.Persistence.Repositories;

internal sealed class RoleRepository : TenantAwareRepository<Role, IdentityDbContext>, IRoleRepository
{
    public RoleRepository(IdentityDbContext context, ITenantContext tenant)
        : base(context, tenant) { }

    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await Query
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<Role?> GetByNormalizedNameAsync(string normalizedName, CancellationToken ct = default)
        => await Query
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.NormalizedName == normalizedName, ct);

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken ct = default)
        => await Query
            .Include(r => r.Permissions)
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

    public async Task<bool> ExistsByNameAsync(string normalizedName, CancellationToken ct = default)
        => await Query.AnyAsync(r => r.NormalizedName == normalizedName, ct);

    public async Task AddAsync(Role role, CancellationToken ct = default)
        => await Context.Roles.AddAsync(role, ct);

    public void Update(Role role)
        => Context.Roles.Update(role);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => Context.SaveChangesAsync(ct);
}
