using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Commands.UploadInstanceAttachment;

/// <summary>
/// Uploads a file and attaches it directly to a workflow instance (not tied to any step).
/// Use this for job-level documents — e.g. a signed contract, purchase order, or site survey report.
/// Returns the new <see cref="Domain.Entities.Attachment"/> ID on success.
/// </summary>
public sealed record UploadInstanceAttachmentCommand(
    Guid   TenantId,
    Guid   WorkflowInstanceId,
    Stream Content,
    string FileName,
    string ContentType,
    long   FileSizeBytes,
    Guid   UploadedBy) : IRequest<Result<Guid>>;
