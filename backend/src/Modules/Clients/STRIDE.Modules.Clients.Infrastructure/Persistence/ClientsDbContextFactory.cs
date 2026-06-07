using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace STRIDE.Modules.Clients.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by `dotnet ef migrations add` when no DI host is available.
/// </summary>
public sealed class ClientsDbContextFactory : IDesignTimeDbContextFactory<ClientsDbContext>
{
    public ClientsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ClientsDbContext>()
            .UseSqlServer(
                "Server=LAPTOP-417EMKN1\\SQLEXPRESS;Database=STRIDE;Integrated Security=true;TrustServerCertificate=true;",
                sql => sql.MigrationsAssembly(typeof(ClientsDbContext).Assembly.FullName))
            .Options;

        return new ClientsDbContext(options);
    }
}
