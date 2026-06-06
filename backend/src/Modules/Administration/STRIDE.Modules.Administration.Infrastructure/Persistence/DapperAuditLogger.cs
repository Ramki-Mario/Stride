using Dapper;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Infrastructure.Persistence;

namespace STRIDE.Modules.Administration.Infrastructure.Persistence;

/// <summary>
/// Dapper-backed implementation of <see cref="IAuditLogger"/>.
///
/// Uses its own connection (via <see cref="IDbConnectionFactory"/>) so it is
/// completely decoupled from the calling handler's EF Core unit of work.
/// All exceptions are caught and logged — never propagated to the caller.
/// CancellationToken.None is used internally so the write succeeds even if
/// the parent HTTP request was cancelled mid-flight.
/// </summary>
internal sealed class DapperAuditLogger : IAuditLogger
{
    private readonly IDbConnectionFactory          _db;
    private readonly ILogger<DapperAuditLogger>    _logger;

    public DapperAuditLogger(IDbConnectionFactory db, ILogger<DapperAuditLogger> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task LogAsync(AuditLogEntry entry)
    {
        try
        {
            // CancellationToken.None — audit writes must complete even when the
            // parent request is cancelled (e.g. client disconnect mid-mutation).
            await using var conn = await _db.OpenConnectionAsync(CancellationToken.None);

            await conn.ExecuteAsync(
                new CommandDefinition(
                    """
                    INSERT INTO [administration].[AuditLogs]
                        (Id, TenantId, ActorId, ActorEmail, Action, ResourceType,
                         ResourceId, OldValueJson, NewValueJson, Timestamp)
                    VALUES
                        (@Id, @TenantId, @ActorId, @ActorEmail, @Action, @ResourceType,
                         @ResourceId, @OldValueJson, @NewValueJson, @Timestamp)
                    """,
                    new
                    {
                        Id           = Guid.NewGuid(),
                        TenantId     = entry.TenantId,
                        ActorId      = entry.ActorId,
                        ActorEmail   = entry.ActorEmail,
                        Action       = entry.Action,
                        ResourceType = entry.ResourceType,
                        ResourceId   = entry.ResourceId,
                        OldValueJson = entry.OldValueJson,
                        NewValueJson = entry.NewValueJson,
                        Timestamp    = DateTime.UtcNow,
                    },
                    cancellationToken: CancellationToken.None));
        }
        catch (Exception ex)
        {
            // Fire-and-forget: audit failures must never surface as command failures.
            _logger.LogWarning(ex,
                "Audit log write failed for action {Action} by actor {ActorId}",
                entry.Action, entry.ActorId);
        }
    }
}
