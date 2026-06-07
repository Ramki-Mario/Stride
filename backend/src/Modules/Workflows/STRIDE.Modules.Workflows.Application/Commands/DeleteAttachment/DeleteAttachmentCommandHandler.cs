using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.Abstractions;

namespace STRIDE.Modules.Workflows.Application.Commands.DeleteAttachment;

internal sealed class DeleteAttachmentCommandHandler
    : IRequestHandler<DeleteAttachmentCommand, Result>
{
    private readonly IAttachmentRepository _repo;
    private readonly IFileStorageService   _storage;

    public DeleteAttachmentCommandHandler(IAttachmentRepository repo, IFileStorageService storage)
    {
        _repo    = repo;
        _storage = storage;
    }

    public async Task<Result> Handle(DeleteAttachmentCommand cmd, CancellationToken cancellationToken)
    {
        var attachment = await _repo.GetByIdAsync(cmd.AttachmentId, cancellationToken);

        if (attachment is null)
            return Result.Failure($"Attachment '{cmd.AttachmentId}' not found.");

        // ── Permission check ───────────────────────────────────────────────────
        var isUploader = attachment.CreatedBy == cmd.DeletedBy;
        if (!isUploader && !cmd.IsManagerOrAdmin)
            return Result.Failure("Only the uploader or a manager can delete an attachment.");

        // ── Soft-delete the entity row ─────────────────────────────────────────
        attachment.SoftDelete();
        await _repo.SaveChangesAsync(cancellationToken);

        // ── Remove the physical file ───────────────────────────────────────────
        // Best-effort: if storage deletion fails we still return success because
        // the entity is already soft-deleted. Orphaned blobs can be cleaned up
        // by a background job if needed.
        try
        {
            await _storage.DeleteAsync(attachment.StorageKey, cancellationToken);
        }
        catch (Exception)
        {
            // Swallow — physical cleanup failure is non-fatal.
        }

        return Result.Success();
    }
}
