using Microsoft.EntityFrameworkCore;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;

namespace STRIDE.Modules.Workflows.Infrastructure.Persistence.Repositories;

internal sealed class ApprovalRequestRepository : IApprovalRequestRepository
{
    private readonly WorkflowsDbContext _context;
    private readonly ITenantContext     _tenant;

    public ApprovalRequestRepository(WorkflowsDbContext context, ITenantContext tenant)
    {
        _context = context;
        _tenant  = tenant;
    }

    public Task<ApprovalRequest?> GetPendingByStepInstanceIdAsync(
        Guid stepInstanceId,
        CancellationToken cancellationToken = default)
        => _context.ApprovalRequests
            .FirstOrDefaultAsync(
                a => a.TenantId == _tenant.TenantId
                  && a.StepInstanceId == stepInstanceId
                  && a.Status == ApprovalStatus.Pending,
                cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
