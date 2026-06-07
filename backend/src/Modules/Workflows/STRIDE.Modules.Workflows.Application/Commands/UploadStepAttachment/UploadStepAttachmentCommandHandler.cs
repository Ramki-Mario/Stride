using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Application.Commands.UploadStepAttachment;

internal sealed class UploadStepAttachmentCommandHandler
    : IRequestHandler<UploadStepAttachmentCommand, Result<Guid>>
{
    /// <summary>
    /// Server-side MIME type allowlist.  Client-side validation should mirror this,
    /// but the server is the authoritative check — never rely on the client alone.
    /// </summary>
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "text/plain",
    };

    private const long MaxFileSizeBytes = 10L * 1024 * 1024; // 10 MB

    private readonly IFileStorageService  _storage;
    private readonly IAttachmentRepository _repo;

    public UploadStepAttachmentCommandHandler(
        IFileStorageService storage,
        IAttachmentRepository repo)
    {
        _storage = storage;
        _repo    = repo;
    }

    public async Task<Result<Guid>> Handle(
        UploadStepAttachmentCommand cmd,
        CancellationToken           cancellationToken)
    {
        // ── Validation ─────────────────────────────────────────────────────────

        if (!AllowedMimeTypes.Contains(cmd.ContentType))
            return Result.Failure<Guid>(
                $"File type '{cmd.ContentType}' is not permitted. " +
                "Accepted types: image/jpeg, image/png, image/webp, " +
                "application/pdf, application/msword, text/plain.");

        if (cmd.FileSizeBytes > MaxFileSizeBytes)
            return Result.Failure<Guid>(
                $"File size ({cmd.FileSizeBytes:N0} bytes) exceeds the 10 MB limit.");

        if (string.IsNullOrWhiteSpace(cmd.FileName))
            return Result.Failure<Guid>("File name cannot be empty.");

        // ── Upload to storage ──────────────────────────────────────────────────

        var storageKey = await _storage.UploadAsync(
            cmd.TenantId,
            cmd.Content,
            cmd.FileName,
            cmd.ContentType,
            cancellationToken);

        // ── Persist entity ─────────────────────────────────────────────────────

        var attachment = Attachment.Create(
            tenantId:           cmd.TenantId,
            workflowInstanceId: cmd.WorkflowInstanceId,
            stepInstanceId:     cmd.StepInstanceId,
            fileName:           cmd.FileName,
            contentType:        cmd.ContentType,
            storageKey:         storageKey,
            fileSizeBytes:      cmd.FileSizeBytes,
            uploadedByUserId:   cmd.UploadedBy);

        await _repo.AddAsync(attachment, cancellationToken);
        await _repo.SaveChangesAsync(cancellationToken);

        return Result.Success(attachment.Id);
    }
}
