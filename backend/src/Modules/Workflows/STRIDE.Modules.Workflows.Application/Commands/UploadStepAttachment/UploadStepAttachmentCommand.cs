using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Commands.UploadStepAttachment;

/// <summary>
/// Uploads a file and attaches it to a specific step instance.
/// Returns the new <see cref="Attachment"/> ID on success.
/// </summary>
public sealed record UploadStepAttachmentCommand(
    Guid   TenantId,
    Guid   WorkflowInstanceId,
    Guid   StepInstanceId,
    Stream Content,
    string FileName,
    string ContentType,
    long   FileSizeBytes,
    Guid   UploadedBy) : IRequest<Result<Guid>>;
