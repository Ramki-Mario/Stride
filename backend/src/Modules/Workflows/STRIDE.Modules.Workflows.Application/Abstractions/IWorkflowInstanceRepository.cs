using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Application.Abstractions;

/// <summary>
/// Repository for <see cref="WorkflowInstance"/> aggregate roots.
/// All read methods are automatically scoped to the current tenant.
/// </summary>
public interface IWorkflowInstanceRepository
{
    /// <summary>Returns a workflow instance by primary key, including its step instances.</summary>
    Task<WorkflowInstance?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a workflow instance for the public (unauthenticated) job view.
    /// Bypasses the current-tenant filter and uses an explicit <paramref name="tenantId"/>
    /// derived from the resolved <see cref="STRIDE.Modules.Workflows.Domain.Entities.SharedWorkflowLink"/> (US-178).
    /// </summary>
    Task<WorkflowInstance?> GetByIdPublicAsync(Guid instanceId, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>Returns all instances for a given workflow definition (history view).</summary>
    Task<IReadOnlyList<WorkflowInstance>> GetByDefinitionIdAsync(Guid definitionId, CancellationToken cancellationToken = default);

    /// <summary>Returns all instances in the current tenant (for list/dashboard views).</summary>
    Task<IReadOnlyList<WorkflowInstance>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Stages a new instance for insertion. Caller must call SaveChangesAsync.</summary>
    Task AddAsync(WorkflowInstance instance, CancellationToken cancellationToken = default);

    /// <summary>Marks an existing instance as modified. Caller must call SaveChangesAsync.</summary>
    void Update(WorkflowInstance instance);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
