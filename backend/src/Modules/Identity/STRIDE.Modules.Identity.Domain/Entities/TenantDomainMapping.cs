using STRIDE.BuildingBlocks.Domain.Entities;

namespace STRIDE.Modules.Identity.Domain.Entities;

public sealed class TenantDomainMapping : AuditableEntity
{
    public string CorporateDomain { get; private set; } = string.Empty;

    private TenantDomainMapping() { }

    internal static TenantDomainMapping Create(Guid tenantId, string corporateDomain, Guid createdBy)
    {
        return new TenantDomainMapping
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CorporateDomain = corporateDomain,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    internal void Remove()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
