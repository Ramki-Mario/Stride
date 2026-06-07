using Microsoft.Extensions.Logging;

namespace STRIDE.Modules.Identity.Infrastructure;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "[DevSeed] Tenant '{Slug}' created ({Id})")]
    internal static partial void DevSeedTenantCreated(this ILogger logger, string slug, Guid id);

    [LoggerMessage(Level = LogLevel.Information, Message = "[DevSeed] Role '{Role}' created ({Id})")]
    internal static partial void DevSeedRoleCreated(this ILogger logger, string role, Guid id);

    [LoggerMessage(Level = LogLevel.Information, Message = "[DevSeed] User '{Email}' created ({Id})")]
    internal static partial void DevSeedUserCreated(this ILogger logger, string email, Guid id);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "[DevSeed] UserRole seeded (userId={U}, roleId={R})")]
    internal static partial void DevSeedUserRoleSeeded(this ILogger logger, Guid u, Guid r);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "[DevSeed] UserTenantMapping seeded (userId={U})")]
    internal static partial void DevSeedMappingSeeded(this ILogger logger, Guid u);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "[DevSeed] ✓ Dev seed complete — login with: {Email} (password in appsettings.Development.json)")]
    internal static partial void DevSeedComplete(this ILogger logger, string email);
}
