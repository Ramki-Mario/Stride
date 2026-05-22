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

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await Query
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken ct = default)
        => await Query
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, ct);

    public async Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken ct = default)
        => await Query.AnyAsync(u => u.NormalizedEmail == normalizedEmail, ct);

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default)
        => await Query
            .Include(u => u.Roles)
            .OrderBy(u => u.DisplayName)
            .ToListAsync(ct);

    public async Task AddAsync(User user, CancellationToken ct = default)
        => await Context.Users.AddAsync(user, ct);

    public async Task AddTenantMappingAsync(UserTenantMapping mapping, CancellationToken ct = default)
        => await Context.UserTenantMappings.AddAsync(mapping, ct);

    public void Update(User user)
        => Context.Users.Update(user);

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => Context.SaveChangesAsync(ct);
}
