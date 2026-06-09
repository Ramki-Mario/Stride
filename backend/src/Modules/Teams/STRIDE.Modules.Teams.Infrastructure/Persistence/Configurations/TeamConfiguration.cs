using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using STRIDE.Modules.Teams.Domain.Entities;

namespace STRIDE.Modules.Teams.Infrastructure.Persistence.Configurations;

internal sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("Teams");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(Team.NameMaxLength);

        builder.Property(t => t.Description)
            .HasMaxLength(Team.DescriptionMaxLength);

        builder.Property(t => t.Status)
            .IsRequired();

        builder.Property(t => t.ParentTeamId);

        // Unique team name per tenant (only among non-deleted rows)
        builder.HasIndex(t => new { t.TenantId, t.Name })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Self-referential FK — no cascade to avoid cycles
        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(t => t.ParentTeamId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.NoAction);

        // Covering indexes for the read queries
        builder.HasIndex(t => new { t.TenantId, t.IsDeleted });
        builder.HasIndex(t => new { t.TenantId, t.Status });
        builder.HasIndex(t => new { t.TenantId, t.CreatedAt });

        builder.Ignore(t => t.DomainEvents);
    }
}
