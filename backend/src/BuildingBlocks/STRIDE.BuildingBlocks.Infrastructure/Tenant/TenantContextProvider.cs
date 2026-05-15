using STRIDE.BuildingBlocks.Application.Abstractions;

namespace STRIDE.BuildingBlocks.Infrastructure.Tenant;

public sealed class TenantContextProvider : ITenantContext, ITenantContextSetter
{
    private Guid? _tenantId;

    public Guid TenantId => _tenantId ?? throw new InvalidOperationException(
        "Tenant context has not been initialized for this request.");

    /// <summary>Called by <see cref="TenantMiddleware"/> for authenticated requests.</summary>
    public void Set(Guid tenantId) => _tenantId = tenantId;

    /// <inheritdoc cref="ITenantContextSetter"/>
    void ITenantContextSetter.SetTenantId(Guid tenantId) => Set(tenantId);
}
