using Microsoft.Extensions.Logging;

namespace STRIDE.BFF.Controllers;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information,
        Message = "[BFF] Tenant registration complete — userId={UserId} tenantId={TenantId}")]
    internal static partial void TenantRegistrationComplete(this ILogger logger, Guid userId, Guid tenantId);
}
