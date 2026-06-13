using Microsoft.EntityFrameworkCore;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Infrastructure.Persistence;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Infrastructure.Persistence.Repositories;

internal sealed class SharedWorkflowLinkRepository
    : TenantAwareRepository<SharedWorkflowLink, WorkflowsDbContext>,
      ISharedWorkflowLinkRepository
{
    public SharedWorkflowLinkRepository(WorkflowsDbContext context, ITenantContext tenant)
        : base(context, tenant) { }

    public async Task<SharedWorkflowLink?> GetByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
        => await Query.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task<SharedWorkflowLink?> GetByTokenAsync(
        string token, CancellationToken cancellationToken = default)
        // Intentionally bypasses the tenant-scoped Query — the token IS the credential;
        // tenant is unknown until after this resolves.
        => await Context.SharedWorkflowLinks
            .FirstOrDefaultAsync(l => l.Token == token && !l.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<SharedWorkflowLink>> ListActiveByInstanceAsync(
        Guid instanceId, CancellationToken cancellationToken = default)
    {
        var links = await Query
            .Where(l => l.WorkflowInstanceId == instanceId && l.RevokedAt == null)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

        return links.AsReadOnly();
    }

    public async Task AddAsync(SharedWorkflowLink link, CancellationToken cancellationToken = default)
        => await Context.SharedWorkflowLinks.AddAsync(link, cancellationToken);

    public void Update(SharedWorkflowLink link)
        => Context.SharedWorkflowLinks.Update(link);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => Context.SaveChangesAsync(cancellationToken);
}
