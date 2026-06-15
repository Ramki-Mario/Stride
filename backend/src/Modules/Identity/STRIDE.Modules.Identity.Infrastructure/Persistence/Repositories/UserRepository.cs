using Microsoft.EntityFrameworkCore;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Infrastructure.Persistence;
using STRIDE.Modules.Identity.Application.Abstractions;
using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository : TenantAwareRepository<User, IdentityDbContext>, IUserRepository
{
    public UserRepository(IdentityDbContext context, ITenantContext tenant)
        : base(context, tenant) { }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await Query
            .Include(u => u.Roles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<User?> GetByIdWithRolePermissionsAsync(Guid id, CancellationToken cancellationToken = default)
        => await Query
            .Include(u => u.Roles.Where(ur => !ur.IsDeleted))
                .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r!.Permissions.Where(rp => !rp.IsDeleted))
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        => await Query
            .Include(u => u.Roles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

    public async Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        => await Query.AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
        => await Query
            .Include(u => u.Roles).ThenInclude(ur => ur.Role)
            .OrderBy(u => u.DisplayName)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
        => await Context.Users.AddAsync(user, cancellationToken);

    public async Task AddTenantMappingAsync(UserTenantMapping mapping, CancellationToken cancellationToken = default)
        => await Context.UserTenantMappings.AddAsync(mapping, cancellationToken);

    public void Update(User user)
        => Context.Users.Update(user);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => Context.SaveChangesAsync(cancellationToken);
}
