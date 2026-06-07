using Microsoft.Extensions.Logging;

namespace STRIDE.BuildingBlocks.Application.Behaviours;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Handling {RequestName}")]
    internal static partial void Handling(this ILogger logger, string requestName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Handled {RequestName}")]
    internal static partial void Handled(this ILogger logger, string requestName);
}
