using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace STRIDE.BuildingBlocks.Infrastructure.Tenant;

public sealed class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantMiddleware> _logger;

    public TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, TenantContextProvider tenantProvider)
    {
        // Tenant is resolved from the "tid" claim carried by the validated JWT.
        // For authenticated requests the claim is always present; for anonymous
        // endpoints (e.g. login, registration) there is no JWT yet — the handler
        // initialises the tenant context itself via ITenantContextSetter.
        // [Authorize] on protected endpoints guarantees the claim is present there.
        var tidClaim = context.User.FindFirst("tid")?.Value;

        if (Guid.TryParse(tidClaim, out var tenantId))
            tenantProvider.Set(tenantId);

        await _next(context);
    }
}
