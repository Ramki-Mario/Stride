using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.Clients.Domain.Entities;
using STRIDE.Modules.Clients.Domain.Enums;

namespace STRIDE.Modules.Clients.Infrastructure.Persistence.Configurations;

public sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("Clients");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.ContactPerson).HasMaxLength(200);
        builder.Property(c => c.Email).HasMaxLength(320);
        builder.Property(c => c.Phone).HasMaxLength(50);
        builder.Property(c => c.Address).HasMaxLength(500);
        builder.Property(c => c.Notes).HasMaxLength(2000);
        builder.Property(c => c.Status).IsRequired().HasDefaultValue(ClientStatus.Active);
        builder.Property(c => c.TenantId).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired();
        builder.Property(c => c.CreatedBy).IsRequired();
        builder.Property(c => c.IsDeleted).IsRequired().HasDefaultValue(false);

        // Unique client name per tenant (case-insensitive enforced at app layer; unique index for DB integrity)
        builder.HasIndex(c => new { c.TenantId, c.Name }).IsUnique()
               .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(c => new { c.TenantId, c.IsDeleted });
        builder.HasIndex(c => new { c.TenantId, c.Status });
        builder.HasIndex(c => new { c.TenantId, c.CreatedAt });
    }
}
