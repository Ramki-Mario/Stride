using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using STRIDE.Modules.Identity.Application.Abstractions;
using STRIDE.Modules.Identity.Domain.Entities;
using STRIDE.Modules.Identity.Infrastructure.Persistence;

namespace STRIDE.Modules.Identity.Infrastructure.Persistence.SeedData;

/// <summary>
/// Development-only data seeder.  Creates a ready-to-use tenant + admin user so
/// the app can be exercised immediately after cloning — no manual SQL required.
///
///   Email    : test@gmail.com
///   Password : 1234abcd
///   Role     : Admin
///   Tenant   : STRIDE Demo Tenant
///
/// The seeder is idempotent: it checks for existence before inserting and is a
/// no-op on subsequent runs.  It is skipped entirely outside Development.
/// </summary>
public static class DevDataSeeder
{
    private static readonly Guid SystemActorId = new("00000000-0000-0000-0000-000000000001");

    private const string TestEmail    = "test@gmail.com";
    private const string TestPassword = "1234abcd";
    private const string TenantSlug   = "stride-demo";
    private const string TenantName   = "STRIDE Demo Tenant";

    /// <summary>
    /// Invoke once after <c>app.Build()</c> in <c>Program.cs</c> when running
    /// in the Development environment.
    /// </summary>
    /// <summary>
    /// Returns the seeded (TenantId, UserId) so downstream seeders can reference
    /// the same tenant/user without re-querying.  Returns null outside Development.
    /// </summary>
    public static async Task<(Guid TenantId, Guid UserId)?> SeedAsync(IHost host)
    {
        var env = host.Services.GetRequiredService<IHostEnvironment>();
        if (!env.IsDevelopment()) return null;

        await using var scope  = host.Services.CreateAsyncScope();
        var db                 = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var hasher             = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger             = scope.ServiceProvider
                                      .GetRequiredService<ILogger<IdentityDbContext>>();

        // ── 1. Tenant ─────────────────────────────────────────────────────────
        var tenant = await db.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Slug == TenantSlug);

        if (tenant is null)
        {
            tenant = Tenant.Create(TenantName, TenantSlug, "Free", SystemActorId);
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();
            logger.LogInformation("[DevSeed] Tenant '{Slug}' created ({Id})", TenantSlug, tenant.Id);
        }

        var tenantId = tenant.Id;

        // ── 2. Admin Role ─────────────────────────────────────────────────────
        var adminRole = await db.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r =>
                r.TenantId == tenantId &&
                r.NormalizedName == DefaultRoles.Admin.ToUpperInvariant());

        if (adminRole is null)
        {
            adminRole = Role.Create(tenantId, DefaultRoles.Admin,
                                    "Full administrative access", SystemActorId);
            db.Roles.Add(adminRole);
            await db.SaveChangesAsync();
            logger.LogInformation("[DevSeed] Role 'Admin' created ({Id})", adminRole.Id);
        }

        // ── 3. User ───────────────────────────────────────────────────────────
        var user = await db.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NormalizedEmail == TestEmail.ToUpperInvariant());

        if (user is null)
        {
            var passwordHash = hasher.Hash(TestPassword);
            user = User.Create(tenantId, TestEmail, "Dev Admin", passwordHash, SystemActorId);

            // Clear domain events — no dispatcher is active during seeding.
            user.ClearDomainEvents();

            db.Users.Add(user);
            await db.SaveChangesAsync();
            logger.LogInformation("[DevSeed] User '{Email}' created ({Id})", TestEmail, user.Id);
        }

        var userId = user.Id;

        // ── 4. UserRole ───────────────────────────────────────────────────────
        var userRoleExists = await db.UserRoles
            .IgnoreQueryFilters()
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == adminRole.Id && !ur.IsDeleted);

        if (!userRoleExists)
        {
            db.UserRoles.Add(UserRole.Create(tenantId, userId, adminRole.Id, SystemActorId));
            await db.SaveChangesAsync();
            logger.LogInformation("[DevSeed] UserRole seeded (userId={U}, roleId={R})",
                                   userId, adminRole.Id);
        }

        // ── 5. UserTenantMapping ──────────────────────────────────────────────
        // Required for generic email domains (e.g. gmail.com).
        // The TenantResolver falls back to this table when no corporate-domain
        // mapping exists, joining on NormalizedEmail to locate the tenant.
        var mappingExists = await db.UserTenantMappings
            .IgnoreQueryFilters()
            .AnyAsync(m => m.UserId == userId && m.TenantId == tenantId && !m.IsDeleted);

        if (!mappingExists)
        {
            db.UserTenantMappings.Add(
                UserTenantMapping.Create(tenantId, userId, DefaultRoles.Admin, SystemActorId));
            await db.SaveChangesAsync();
            logger.LogInformation("[DevSeed] UserTenantMapping seeded (userId={U})", userId);
        }

        logger.LogInformation(
            "[DevSeed] ✓ Dev seed complete — login: {Email} / {Password}",
            TestEmail, TestPassword);

        return (tenantId, userId);
    }
}
