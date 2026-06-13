using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.Commands.CreateSharedLink;

namespace STRIDE.Modules.Workflows.Application.Queries.ListSharedLinks;

/// <summary>
/// Lists the active (non-revoked) shared links for a workflow instance, newest first.
/// </summary>
public sealed record ListSharedLinksQuery(Guid WorkflowInstanceId)
    : IRequest<Result<IReadOnlyList<SharedLinkDto>>>;
