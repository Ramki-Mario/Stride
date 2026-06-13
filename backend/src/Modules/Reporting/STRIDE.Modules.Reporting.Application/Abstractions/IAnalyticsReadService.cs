using STRIDE.Modules.Reporting.Application.ReadModels;

namespace STRIDE.Modules.Reporting.Application.Abstractions;

/// <summary>
/// Read-side analytics service: Dapper projections over completed workflow and step instances.
/// All methods are tenant-scoped.
/// </summary>
public interface IAnalyticsReadService
{
    /// <summary>
    /// Returns three datasets for the completion-time analytics view:
    ///   1. Average duration by workflow definition (bar chart).
    ///   2. Average step duration sorted descending (bottleneck horizontal bar).
    ///   3. Average duration grouped by calendar week (trend line).
    /// </summary>
    Task<CompletionTimeAnalyticsDto> GetCompletionTimesAsync(
        Guid      tenantId,
        DateTime  fromDate,
        DateTime  toDate,
        Guid?     workflowDefinitionId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns active (non-archived) workflow definitions for the tenant.
    /// Used to populate the definition filter dropdown on the analytics page.
    /// </summary>
    Task<IReadOnlyList<AnalyticsWorkflowDefinitionDto>> GetWorkflowDefinitionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a flat list of completed instances in the date range for CSV export.
    /// </summary>
    Task<IReadOnlyList<CompletionTimeExportRowDto>> GetCompletionTimesExportAsync(
        Guid      tenantId,
        DateTime  fromDate,
        DateTime  toDate,
        Guid?     workflowDefinitionId = null,
        CancellationToken cancellationToken = default);
}
