using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Commands.DeleteAttachment;

/// <summary>
/// Soft-deletes an attachment and physically removes the stored file.
/// Succeeds only if the caller is the original uploader or has a manager/admin role.
/// </summary>
public sealed record DeleteAttachmentCommand(
    Guid AttachmentId,
    Guid DeletedBy,
    bool IsManagerOrAdmin) : IRequest<Result>;
