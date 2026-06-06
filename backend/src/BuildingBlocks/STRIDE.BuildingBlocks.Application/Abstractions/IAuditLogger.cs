namespace STRIDE.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Cross-cutting abstraction for writing audit trail entries.
///
/// Implementations MUST be fire-and-forget safe: an audit write failure must
/// never propagate to the calling command handler.  Log a warning instead.
///
/// The interface lives in BuildingBlocks so both Administration and Invoicing
/// handlers can inject it without a cross-module dependency.
/// </summary>
public interface IAuditLogger
{
    /// <summary>
    /// Persists a single audit log entry asynchronously.
    /// Guaranteed non-throwing — all exceptions are swallowed and logged.
    /// </summary>
    Task LogAsync(AuditLogEntry entry);
}
