using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.KitOps.Domain.Entities;

namespace STRIDE.Modules.KitOps.Infrastructure.Persistence.Configurations;

internal sealed class KitReservationConfiguration : IEntityTypeConfiguration<KitReservation>
{
    public void Configure(EntityTypeBuilder<KitReservation> builder)
    {
        builder.ToTable("KitReservations");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.KitItemId)
            .IsRequired();

        builder.Property(r => r.RequestedByUserId)
            .IsRequired();

        builder.Property(r => r.Status)
            .IsRequired();

        builder.Property(r => r.Notes)
            .HasMaxLength(KitReservation.NotesMaxLength);

        // FK to KitItems — no cascade delete; reservations are historical records
        builder.HasOne<KitItem>()
            .WithMany()
            .HasForeignKey(r => r.KitItemId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(r => new { r.TenantId, r.KitItemId, r.Status });
        builder.HasIndex(r => new { r.TenantId, r.RequestedByUserId });
        builder.HasIndex(r => new { r.TenantId, r.IsDeleted });
        builder.HasIndex(r => new { r.TenantId, r.CreatedAt });

        // A user may hold only one pending request per kit item — DB-level backstop for the
        // application duplicate guard (Status 0 = Pending). Fulfilled/Cancelled rows are exempt,
        // so the same user can re-request after their previous request is resolved.
        builder.HasIndex(r => new { r.TenantId, r.KitItemId, r.RequestedByUserId })
            .IsUnique()
            .HasFilter("[Status] = 0 AND [IsDeleted] = 0");

        builder.Ignore(r => r.DomainEvents);
    }
}
