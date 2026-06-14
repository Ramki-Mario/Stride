using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace STRIDE.Modules.Scheduling.Infrastructure.Persistence;

internal sealed class SchedulingDbContextFactory : IDesignTimeDbContextFactory<SchedulingDbContext>
{
    public SchedulingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SchedulingDbContext>()
            .UseSqlServer(
                "Server=LAPTOP-417EMKN1\\SQLEXPRESS2025;Database=STRIDE;Integrated Security=true;TrustServerCertificate=true;",
                sql => sql.MigrationsAssembly(typeof(SchedulingDbContext).Assembly.FullName))
            .Options;

        return new SchedulingDbContext(options);
    }
}
