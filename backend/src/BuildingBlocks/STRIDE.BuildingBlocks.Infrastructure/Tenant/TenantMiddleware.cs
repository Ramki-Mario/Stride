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
        // Tenant is resolved from the validated session claim injected by the BFF.
        // The "tid" claim is set by the BFF after validating the session cookie in Redis.
        var tidClaim = context.User.FindFirst("tid")?.Value;

        if (!Guid.TryParse(tidClaim, out var tenantId))
        {
            _logger.LogWarning("Request rejected: missing or invalid tenant claim.");
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        tenantProvider.Set(tenantId);
        await _next(context);
    }
}
