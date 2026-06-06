using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Application.Abstractions;

/// <summary>
/// Repository for <see cref="WorkflowDefinition"/> aggregate roots.
/// All read methods are automatically scoped to the current tenant
/// via <c>TenantAwareRepository</c> in the infrastructure layer.
/// </summary>
public interface IWorkflowDefinitionRepository
{
    /// <summary>Returns a workflow definition by primary key within the current tenant.</summary>
    Task<WorkflowDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns all non-deleted workflow definitions in the current tenant.</summary>
    Task<IReadOnlyList<WorkflowDefinition>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns true if a non-deleted definition with the given name exists in this tenant.</summary>
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Stages a new definition for insertion. Caller must call SaveChangesAsync.</summary>
    Task AddAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);

    /// <summary>Marks an existing definition as modified. Caller must call SaveChangesAsync.</summary>
    void Update(WorkflowDefinition definition);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
