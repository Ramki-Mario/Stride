namespace STRIDE.Modules.Reporting.Application.Abstractions;

/// <summary>
/// Pushes a real-time dashboard update for a tenant. Implementations must be
/// fire-and-forget safe: a failed publish is logged, never thrown — a dashboard
/// refresh must not roll back the business operation that triggered it.
/// </summary>
public interface IDashboardNotifier
{
    Task PublishAsync(Guid tenantId, string eventType, CancellationToken cancellationToken = default);
}
