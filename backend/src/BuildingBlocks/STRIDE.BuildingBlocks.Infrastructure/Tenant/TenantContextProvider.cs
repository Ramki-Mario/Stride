using STRIDE.BuildingBlocks.Application.Abstractions;

namespace STRIDE.BuildingBlocks.Infrastructure.Tenant;

public sealed class TenantContextProvider : ITenantContext
{
    private Guid? _tenantId;

    public Guid TenantId => _tenantId ?? throw new InvalidOperationException(
        "Tenant context has not been initialized for this request.");

    public void Set(Guid tenantId) => _tenantId = tenantId;
}
