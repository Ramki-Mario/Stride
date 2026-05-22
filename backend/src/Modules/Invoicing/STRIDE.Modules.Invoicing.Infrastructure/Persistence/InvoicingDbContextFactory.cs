using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace STRIDE.Modules.Invoicing.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by `dotnet ef migrations add` when no DI host is available.
/// </summary>
public sealed class InvoicingDbContextFactory : IDesignTimeDbContextFactory<InvoicingDbContext>
{
    public InvoicingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<InvoicingDbContext>()
            .UseSqlServer(
                "Server=LAPTOP-417EMKN1\\SQLEXPRESS;Database=STRIDE;Integrated Security=true;TrustServerCertificate=true;",
                sql => sql.MigrationsAssembly(typeof(InvoicingDbContext).Assembly.FullName))
            .Options;

        return new InvoicingDbContext(options);
    }
}
