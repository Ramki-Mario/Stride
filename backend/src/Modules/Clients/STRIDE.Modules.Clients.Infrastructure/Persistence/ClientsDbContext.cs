using Microsoft.EntityFrameworkCore;
using STRIDE.Modules.Clients.Domain.Entities;

namespace STRIDE.Modules.Clients.Infrastructure.Persistence;

public sealed class ClientsDbContext : DbContext
{
    public DbSet<Client> Clients { get; set; } = null!;

    public ClientsDbContext(DbContextOptions<ClientsDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("clients");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClientsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
