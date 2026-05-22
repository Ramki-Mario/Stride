using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace STRIDE.BuildingBlocks.Infrastructure.Logging;

/// <summary>
/// HTTP middleware that pushes per-request Serilog context properties after
/// authentication and tenant resolution have run.
///
/// Properties added to every structured log line for authenticated requests:
///   UserId   — the "sub" JWT claim (user's GUID)
///   TenantId — the "tid" JWT claim (tenant's GUID)
///
/// Runs AFTER <c>UseAuthentication()</c> and <c>TenantMiddleware</c> so both
/// claims are already populated in <see cref="HttpContext.User"/>.
///
/// Works alongside <see cref="Correlation.CorrelationIdMiddleware"/> which
/// already enriches CorrelationId.
/// </summary>
public sealed class SerilogEnrichmentMiddleware
{
    private readonly RequestDelegate _next;

    public SerilogEnrichmentMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var user = context.User;

        var userIdRaw   = user.FindFirst("sub")?.Value;
        var tenantIdRaw = user.FindFirst("tid")?.Value;

        // Push both properties for the lifetime of this request.
        // LogContext.PushProperty is stack-based; the using block pops on exit.
        using var _ = Guid.TryParse(userIdRaw, out var userId)
            ? LogContext.PushProperty("UserId", userId)
            : null;

        using var __ = Guid.TryParse(tenantIdRaw, out var tenantId)
            ? LogContext.PushProperty("TenantId", tenantId)
            : null;

        await _next(context);
    }
}
