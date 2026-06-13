using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.Abstractions;

namespace STRIDE.Modules.Workflows.Application.Commands.RevokeSharedLink;

internal sealed class RevokeSharedLinkCommandHandler
    : IRequestHandler<RevokeSharedLinkCommand, Result>
{
    private readonly ISharedWorkflowLinkRepository _links;

    public RevokeSharedLinkCommandHandler(ISharedWorkflowLinkRepository links)
        => _links = links;

    public async Task<Result> Handle(RevokeSharedLinkCommand request, CancellationToken cancellationToken)
    {
        var link = await _links.GetByIdAsync(request.LinkId, cancellationToken);
        if (link is null)
            return Result.Failure($"Shared link '{request.LinkId}' not found.");

        // Guard against revoking a link that belongs to a different instance (IDOR defence —
        // the link id is tenant-scoped by the repo, this also pins it to the route's instance).
        if (link.WorkflowInstanceId != request.WorkflowInstanceId)
            return Result.Failure($"Shared link '{request.LinkId}' not found.");

        link.Revoke();
        _links.Update(link);
        await _links.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
