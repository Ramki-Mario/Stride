using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace STRIDE.Modules.Reporting.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by EF Core tooling (dotnet ef migrations add).
/// Not used at runtime — the real DbContext is registered via DI in ReportingInfrastructureExtensions.
/// </summary>
internal sealed class ReportingDbContextFactory : IDesignTimeDbContextFactory<ReportingDbContext>
{
    public ReportingDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ReportingDbContext>();

        optionsBuilder.UseSqlServer(
            "Server=LAPTOP-417EMKN1\\SQLEXPRESS;Database=STRIDE;Integrated Security=true;TrustServerCertificate=true;",
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "reporting"));

        return new ReportingDbContext(optionsBuilder.Options);
    }
}
