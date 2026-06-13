using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Commands.RevokeSharedLink;

/// <summary>
/// Revokes a shared link, immediately invalidating it for any future public access.
/// </summary>
public sealed record RevokeSharedLinkCommand(
    Guid WorkflowInstanceId,
    Guid LinkId) : IRequest<Result>;
