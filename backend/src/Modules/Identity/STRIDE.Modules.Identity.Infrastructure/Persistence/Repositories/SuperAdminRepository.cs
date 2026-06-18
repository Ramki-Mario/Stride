using Microsoft.EntityFrameworkCore;
using STRIDE.Modules.Identity.Application.Abstractions;

namespace STRIDE.Modules.Identity.Infrastructure.Persistence.Repositories;

internal sealed class SuperAdminRepository : ISuperAdminRepository
{
    private readonly IdentityDbContext _context;

    public SuperAdminRepository(IdentityDbContext context) => _context = context;

    public Task<bool> IsSuperAdminAsync(string email, CancellationToken cancellationToken = default)
        => _context.SuperAdmins.AnyAsync(
            s => s.NormalizedEmail == email.Trim().ToUpperInvariant(),
            cancellationToken);
}
