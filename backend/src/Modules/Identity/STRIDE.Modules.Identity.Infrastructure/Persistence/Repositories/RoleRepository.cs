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

    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await Query
            .Include(r => r.Permissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<Role?> GetByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default)
        => await Query
            .Include(r => r.Permissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.NormalizedName == normalizedName, cancellationToken);

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default)
        => await Query
            .Include(r => r.Permissions).ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetAssignedUserIdsAsync(
        Guid roleId, CancellationToken cancellationToken = default)
        => await Context.UserRoles
            .AsNoTracking()
            .Where(ur => ur.RoleId == roleId && ur.TenantId == Tenant.TenantId && !ur.IsDeleted)
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

    public async Task<bool> ExistsByNameAsync(string normalizedName, CancellationToken cancellationToken = default)
        => await Query.AnyAsync(r => r.NormalizedName == normalizedName, cancellationToken);

    public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
        => await Context.Roles.AddAsync(role, cancellationToken);

    public void Update(Role role)
        => Context.Roles.Update(role);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => Context.SaveChangesAsync(cancellationToken);
}
