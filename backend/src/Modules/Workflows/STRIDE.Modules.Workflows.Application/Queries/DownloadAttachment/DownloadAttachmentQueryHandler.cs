using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.Abstractions;

namespace STRIDE.Modules.Workflows.Application.Queries.DownloadAttachment;

internal sealed class DownloadAttachmentQueryHandler
    : IRequestHandler<DownloadAttachmentQuery, Result<AttachmentDownloadResult>>
{
    private readonly IAttachmentRepository _repo;
    private readonly IFileStorageService   _storage;

    public DownloadAttachmentQueryHandler(IAttachmentRepository repo, IFileStorageService storage)
    {
        _repo    = repo;
        _storage = storage;
    }

    public async Task<Result<AttachmentDownloadResult>> Handle(
        DownloadAttachmentQuery query,
        CancellationToken       cancellationToken)
    {
        var attachment = await _repo.GetByIdAsync(query.AttachmentId, cancellationToken);

        if (attachment is null)
            return Result.Failure<AttachmentDownloadResult>(
                $"Attachment '{query.AttachmentId}' not found.");

        var stream = await _storage.DownloadAsync(attachment.StorageKey, cancellationToken);

        return Result.Success(new AttachmentDownloadResult(
            Content:     stream,
            FileName:    attachment.FileName,
            ContentType: attachment.ContentType));
    }
}
