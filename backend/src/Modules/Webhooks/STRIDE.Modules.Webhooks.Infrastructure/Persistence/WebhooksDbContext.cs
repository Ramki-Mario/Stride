using Microsoft.EntityFrameworkCore;
using STRIDE.Modules.Webhooks.Application.Abstractions;
using STRIDE.Modules.Webhooks.Domain.Entities;

namespace STRIDE.Modules.Webhooks.Infrastructure.Persistence;

public sealed class WebhooksDbContext : DbContext
{
    private readonly IWebhookSecretProtector _protector;

    public DbSet<WebhookSubscription> WebhookSubscriptions { get; set; } = null!;

    public WebhooksDbContext(DbContextOptions<WebhooksDbContext> options, IWebhookSecretProtector protector)
        : base(options)
        => _protector = protector;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("webhooks");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WebhooksDbContext).Assembly);

        // Encrypt the signing secret at rest. The converter closes over the
        // injected protector so plaintext never touches the database — the domain
        // stays oblivious to encryption (a persistence concern).
        modelBuilder.Entity<WebhookSubscription>()
            .Property(s => s.SigningSecret)
            .HasConversion(
                plain  => _protector.Protect(plain),
                stored => _protector.Unprotect(stored));

        base.OnModelCreating(modelBuilder);
    }
}
