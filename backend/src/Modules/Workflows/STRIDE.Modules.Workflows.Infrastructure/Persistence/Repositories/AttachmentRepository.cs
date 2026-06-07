using Microsoft.EntityFrameworkCore;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Infrastructure.Persistence;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Infrastructure.Persistence.Repositories;

internal sealed class AttachmentRepository
    : TenantAwareRepository<Attachment, WorkflowsDbContext>,
      IAttachmentRepository
{
    public AttachmentRepository(WorkflowsDbContext context, ITenantContext tenant)
        : base(context, tenant) { }

    public async Task<Attachment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await Query.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Attachment>> GetByStepInstanceIdAsync(
        Guid stepInstanceId,
        CancellationToken cancellationToken = default)
        => await Query
            .Where(a => a.StepInstanceId == stepInstanceId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Attachment>> GetByWorkflowInstanceIdAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default)
        => await Query
            .Where(a => a.WorkflowInstanceId == workflowInstanceId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Attachment attachment, CancellationToken cancellationToken = default)
        => await Context.Attachments.AddAsync(attachment, cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => Context.SaveChangesAsync(cancellationToken);
}
