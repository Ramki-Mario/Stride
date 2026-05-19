using Microsoft.AspNetCore.Http;
using STRIDE.BuildingBlocks.Application.Abstractions;

namespace STRIDE.BuildingBlocks.Infrastructure.Tenant;

/// <summary>
/// Scoped tenant context.  Resolves the current tenant's ID via two paths:
///
///   1. Explicit set — <see cref="ITenantContextSetter.SetTenantId"/> is called
///      by <see cref="TenantMiddleware"/> (eager, per-request) or directly by
///      <c>LoginCommandHandler</c> for the anonymous login flow (no JWT yet).
///
///   2. Lazy JWT fallback — if the value was never explicitly set, <see cref="TenantId"/>
///      reads the <c>"tid"</c> claim straight from the current request's
///      <see cref="ClaimsPrincipal"/> via <see cref="IHttpContextAccessor"/>.
///      This makes the class resilient to middleware-ordering issues and removes
///      the strict requirement that <see cref="TenantMiddleware"/> run before any
///      code that accesses <see cref="ITenantContext"/>.
/// </summary>
public sealed class TenantContextProvider : ITenantContext, ITenantContextSetter
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private Guid? _tenantId;

    public TenantContextProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid TenantId
    {
        get
        {
            // 1. Explicit value set by middleware or LoginCommandHandler.
            if (_tenantId.HasValue)
                return _tenantId.Value;

            // 2. Lazy fallback: read "tid" claim directly from the JWT claims
            //    already loaded into the current request's ClaimsPrincipal.
            var raw = _httpContextAccessor.HttpContext?
                          .User.FindFirst("tid")?.Value;

            if (Guid.TryParse(raw, out var fromClaim))
            {
                _tenantId = fromClaim; // cache for repeated access
                return fromClaim;
            }

            throw new InvalidOperationException(
                "Tenant context has not been initialized for this request. " +
                "Ensure the request carries a valid JWT with a 'tid' claim " +
                "or that ITenantContextSetter.SetTenantId() was called.");
        }
    }

    /// <summary>Called by <see cref="TenantMiddleware"/> (eager path).</summary>
    public void Set(Guid tenantId) => _tenantId = tenantId;

    /// <inheritdoc cref="ITenantContextSetter"/>
    void ITenantContextSetter.SetTenantId(Guid tenantId) => Set(tenantId);
}
