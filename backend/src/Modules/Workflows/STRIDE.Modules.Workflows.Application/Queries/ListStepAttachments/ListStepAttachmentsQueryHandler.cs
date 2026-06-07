using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.DTOs;

namespace STRIDE.Modules.Workflows.Application.Queries.ListStepAttachments;

internal sealed class ListStepAttachmentsQueryHandler
    : IRequestHandler<ListStepAttachmentsQuery, Result<IReadOnlyList<AttachmentDto>>>
{
    private readonly IAttachmentRepository _repo;

    public ListStepAttachmentsQueryHandler(IAttachmentRepository repo) => _repo = repo;

    public async Task<Result<IReadOnlyList<AttachmentDto>>> Handle(
        ListStepAttachmentsQuery query,
        CancellationToken        cancellationToken)
    {
        var attachments = await _repo.GetByStepInstanceIdAsync(query.StepInstanceId, cancellationToken);

        var dtos = attachments
            .Select(a => new AttachmentDto(
                Id:               a.Id,
                FileName:         a.FileName,
                ContentType:      a.ContentType,
                FileSizeBytes:    a.FileSizeBytes,
                UploadedByUserId: a.CreatedBy,
                CreatedAt:        a.CreatedAt,
                IsImage:          a.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        return Result.Success<IReadOnlyList<AttachmentDto>>(dtos);
    }
}
