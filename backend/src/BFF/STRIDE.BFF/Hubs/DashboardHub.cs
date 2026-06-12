using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace STRIDE.BFF.Hubs;

/// <summary>
/// Browser-facing SignalR hub for real-time dashboard updates (US-176).
///
/// Authenticated by the same HttpOnly session cookie as every BFF endpoint —
/// the browser never sees a JWT (ADR-007/008). On connect, the client joins its
/// tenant group so pushes from <see cref="Realtime.DashboardRedisSubscriber"/>
/// are tenant-isolated. The hub itself exposes no client-invokable methods:
/// it is push-only, and clients refetch the affected panels over HTTP.
/// </summary>
[Authorize]
public sealed class DashboardHub : Hub
{
    /// <summary>Group name for a tenant's connected dashboard clients.</summary>
    public static string TenantGroup(Guid tenantId) => $"tenant:{tenantId:N}";

    public override async Task OnConnectedAsync()
    {
        var tid = Context.User?.FindFirst("tid")?.Value;

        if (Guid.TryParse(tid, out var tenantId) && tenantId != Guid.Empty)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, TenantGroup(tenantId));
        }
        else
        {
            // No usable tenant claim — refuse the connection rather than leaving
            // an authenticated socket outside any group (it would receive nothing,
            // but a dangling connection hides misconfiguration).
            Context.Abort();
        }

        await base.OnConnectedAsync();
    }
}
