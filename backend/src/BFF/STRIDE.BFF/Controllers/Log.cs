using Microsoft.Extensions.Logging;

namespace STRIDE.BFF.Controllers;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information,
        Message = "[BFF] Tenant registration complete — userId={UserId} tenantId={TenantId}")]
    internal static partial void TenantRegistrationComplete(this ILogger logger, Guid userId, Guid tenantId);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "[BFF] Silent token renewal succeeded — userId={UserId}")]
    internal static partial void SilentRenewalSucceeded(this ILogger logger, Guid userId);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "[BFF] Silent token renewal failed — refresh token rejected by Host — userId={UserId}")]
    internal static partial void SilentRenewalFailed(this ILogger logger, Guid userId);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "[BFF] Silent token renewal network error — userId={UserId}")]
    internal static partial void SilentRenewalNetworkError(this ILogger logger, Guid userId, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "[BFF] Silent token renewal lock timeout — userId={UserId}, proceeding with potentially stale token")]
    internal static partial void SilentRenewalLockTimeout(this ILogger logger, Guid userId);
}
