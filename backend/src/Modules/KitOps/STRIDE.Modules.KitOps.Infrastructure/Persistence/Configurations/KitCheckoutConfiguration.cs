using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.KitOps.Domain.Entities;

namespace STRIDE.Modules.KitOps.Infrastructure.Persistence.Configurations;

internal sealed class KitCheckoutConfiguration : IEntityTypeConfiguration<KitCheckout>
{
    public void Configure(EntityTypeBuilder<KitCheckout> builder)
    {
        builder.ToTable("KitCheckouts");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.KitItemId)
            .IsRequired();

        builder.Property(c => c.CheckedOutByUserId)
            .IsRequired();

        builder.Property(c => c.ExpectedReturnAt)
            .IsRequired();

        builder.Property(c => c.ReturnedAt);

        builder.Property(c => c.ReturnedByUserId);

        builder.Property(c => c.Status)
            .IsRequired();

        builder.Property(c => c.Notes)
            .HasMaxLength(KitCheckout.NotesMaxLength);

        // FK to KitItems — no cascade delete; checkouts outlive item deactivation for history
        builder.HasOne<KitItem>()
            .WithMany()
            .HasForeignKey(c => c.KitItemId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(c => new { c.TenantId, c.KitItemId, c.Status });
        builder.HasIndex(c => new { c.TenantId, c.CheckedOutByUserId });
        builder.HasIndex(c => new { c.TenantId, c.IsDeleted });

        builder.Ignore(c => c.DomainEvents);
    }
}
