using STRIDE.Modules.Workflows.Application.DTOs;

namespace STRIDE.Modules.Workflows.Application.Abstractions;

/// <summary>
/// Reads a redacted invoice snapshot for the public job view (US-179).
/// Implemented in Infrastructure via a cross-schema Dapper query.
/// </summary>
public interface IPublicInvoiceService
{
    /// <summary>
    /// Returns the most recent non-deleted invoice linked to the given workflow instance,
    /// or null if none exists.
    /// </summary>
    Task<PublicInvoiceDto?> GetByWorkflowInstanceIdAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default);
}
