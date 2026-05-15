namespace STRIDE.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Allows Application-layer handlers to initialise the ambient tenant context
/// before invoking tenant-filtered repositories.
///
/// Required for anonymous flows (login, registration) where the tenant identity
/// must be resolved inside the handler rather than extracted from a JWT by
/// the <c>TenantMiddleware</c> pipeline step.
/// </summary>
public interface ITenantContextSetter
{
    void SetTenantId(Guid tenantId);
}
