using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Application.Abstractions;

/// <summary>
/// Repository for <see cref="ApprovalRequest"/> entities.
/// Approval requests are owned by the WorkflowInstance aggregate root and are
/// typically loaded via <see cref="IWorkflowInstanceRepository"/> eager-loading.
/// This interface is used for direct lookup by step instance ID.
/// </summary>
public interface IApprovalRequestRepository
{
    Task<ApprovalRequest?> GetPendingByStepInstanceIdAsync(
        Guid stepInstanceId,
        CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
