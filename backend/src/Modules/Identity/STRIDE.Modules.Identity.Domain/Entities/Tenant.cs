using STRIDE.BuildingBlocks.Domain.Entities;

namespace STRIDE.Modules.Identity.Domain.Entities;

public sealed class Tenant : AuditableEntity
{
    private readonly List<TenantDomainMapping> _domainMappings = new();

    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public string Plan { get; private set; } = string.Empty;

    public IReadOnlyList<TenantDomainMapping> DomainMappings => _domainMappings.AsReadOnly();

    private Tenant() { }

    public static Tenant Create(string name, string slug, string plan, Guid createdBy)
    {
        var id = Guid.NewGuid();
        return new Tenant
        {
            Id = id,
            // Tenant entity owns itself — TenantId == Id for the root tenant record
            TenantId = id,
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            Plan = plan.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }

    public void AddCorporateDomain(string domain, Guid createdBy)
    {
        var normalized = domain.Trim().ToLowerInvariant();
        if (_domainMappings.Any(d => d.CorporateDomain == normalized && !d.IsDeleted))
            return;

        _domainMappings.Add(TenantDomainMapping.Create(Id, normalized, createdBy));
        UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveCorporateDomain(string domain)
    {
        var normalized = domain.Trim().ToLowerInvariant();
        _domainMappings
            .FirstOrDefault(d => d.CorporateDomain == normalized && !d.IsDeleted)
            ?.Remove();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePlan(string plan)
    {
        Plan = plan.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
