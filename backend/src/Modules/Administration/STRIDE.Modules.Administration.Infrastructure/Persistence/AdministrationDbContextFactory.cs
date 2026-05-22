using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace STRIDE.Modules.Administration.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by <c>dotnet ef migrations add</c> when no DI host is available.
/// </summary>
public sealed class AdministrationDbContextFactory : IDesignTimeDbContextFactory<AdministrationDbContext>
{
    public AdministrationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AdministrationDbContext>()
            .UseSqlServer(
                "Server=LAPTOP-417EMKN1\\SQLEXPRESS;Database=STRIDE;Integrated Security=true;TrustServerCertificate=true;",
                sql => sql.MigrationsAssembly(typeof(AdministrationDbContext).Assembly.FullName))
            .Options;

        return new AdministrationDbContext(options);
    }
}
