using Microsoft.Extensions.Logging;

namespace STRIDE.Modules.Workflows.Infrastructure;

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information,
        Message = "[WorkflowSeed] ✓ Workflow seed complete — {Count} instances across 3 active definitions.")]
    internal static partial void WorkflowSeedComplete(this ILogger logger, int count);
}
