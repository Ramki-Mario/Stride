using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Commands.CreateSharedLink;

/// <summary>
/// Creates a new shareable link for a workflow instance and returns the full public URL.
/// </summary>
public sealed record CreateSharedLinkCommand(
    Guid TenantId,
    Guid WorkflowInstanceId,
    Guid CreatedByUserId,
    int? ExpiryDays = null) : IRequest<Result<SharedLinkDto>>;
