using STRIDE.BuildingBlocks.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Domain.Entities;

public sealed class ApprovalRequest : BaseEntity<Guid>
{
    public Guid  WorkflowInstanceId  { get; private set; }
    public Guid  StepInstanceId      { get; private set; }
    public Guid  TenantId            { get; private set; }

    /// <summary>Cross-module FK to the Identity role whose members may approve this request.</summary>
    public Guid? RequestedFromRoleId { get; private set; }

    public ApprovalStatus   Status            { get; private set; }
    public RejectionHandling RejectionHandling { get; private set; }

    /// <summary>
    /// When <see cref="RejectionHandling"/> is <see cref="RejectionHandling.RevertToStep"/>,
    /// the Order of the step to reset on rejection.
    /// </summary>
    public int? RevertToStepOrder    { get; private set; }

    public Guid?   DecisionByUserId  { get; private set; }
    public DateTime? DecisionAt      { get; private set; }
    public string? Comment           { get; private set; }
    public DateTime CreatedAt        { get; private set; }

    private ApprovalRequest() { }

    internal static ApprovalRequest Create(
        Guid  workflowInstanceId,
        Guid  stepInstanceId,
        Guid  tenantId,
        Guid? requestedFromRoleId,
        RejectionHandling rejectionHandling,
        int?  revertToStepOrder)
    {
        return new ApprovalRequest
        {
            Id                  = Guid.NewGuid(),
            WorkflowInstanceId  = workflowInstanceId,
            StepInstanceId      = stepInstanceId,
            TenantId            = tenantId,
            RequestedFromRoleId = requestedFromRoleId,
            Status              = ApprovalStatus.Pending,
            RejectionHandling   = rejectionHandling,
            RevertToStepOrder   = revertToStepOrder,
            CreatedAt           = DateTime.UtcNow,
        };
    }

    internal void Approve(Guid approvedBy, string? comment)
    {
        if (Status != ApprovalStatus.Pending)
            throw new WorkflowDomainException("Approval request is not in Pending status.");

        Status           = ApprovalStatus.Approved;
        DecisionByUserId = approvedBy;
        DecisionAt       = DateTime.UtcNow;
        Comment          = comment?.Trim();
    }

    internal void Reject(Guid rejectedBy, string? comment)
    {
        if (Status != ApprovalStatus.Pending)
            throw new WorkflowDomainException("Approval request is not in Pending status.");

        Status           = ApprovalStatus.Rejected;
        DecisionByUserId = rejectedBy;
        DecisionAt       = DateTime.UtcNow;
        Comment          = comment?.Trim();
    }
}
