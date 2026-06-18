using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Infrastructure.Persistence.Configurations;

internal sealed class SuperAdminConfiguration : IEntityTypeConfiguration<SuperAdmin>
{
    public void Configure(EntityTypeBuilder<SuperAdmin> b)
    {
        b.ToTable("SuperAdmins");
        b.HasKey(s => s.Id);
        b.Property(s => s.NormalizedEmail).HasMaxLength(256).IsRequired();
        b.Property(s => s.AddedAt).IsRequired();
        b.HasIndex(s => s.NormalizedEmail).IsUnique();
    }
}
