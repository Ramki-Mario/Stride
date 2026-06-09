using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace STRIDE.Modules.Teams.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by EF migrations CLI.
/// Connection string is only used locally — never committed with credentials.
/// </summary>
public sealed class TeamsDbContextFactory : IDesignTimeDbContextFactory<TeamsDbContext>
{
    public TeamsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TeamsDbContext>()
            .UseSqlServer(
                "Server=LAPTOP-417EMKN1\\SQLEXPRESS;Database=STRIDE;Integrated Security=true;TrustServerCertificate=true;",
                sql => sql.MigrationsAssembly(typeof(TeamsDbContext).Assembly.FullName))
            .Options;

        return new TeamsDbContext(options);
    }
}
