using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.DTOs;

namespace STRIDE.Modules.Workflows.Application.Queries.ListWorkflowAttachments;

internal sealed class ListWorkflowAttachmentsQueryHandler
    : IRequestHandler<ListWorkflowAttachmentsQuery, Result<IReadOnlyList<WorkflowAttachmentDto>>>
{
    private readonly IAttachmentRepository       _attachmentRepo;
    private readonly IWorkflowInstanceRepository _instanceRepo;

    public ListWorkflowAttachmentsQueryHandler(
        IAttachmentRepository       attachmentRepo,
        IWorkflowInstanceRepository instanceRepo)
    {
        _attachmentRepo = attachmentRepo;
        _instanceRepo   = instanceRepo;
    }

    public async Task<Result<IReadOnlyList<WorkflowAttachmentDto>>> Handle(
        ListWorkflowAttachmentsQuery query,
        CancellationToken            cancellationToken)
    {
        var attachments = await _attachmentRepo.GetByWorkflowInstanceIdAsync(
            query.WorkflowInstanceId, cancellationToken);

        // Build a stepId → stepName dictionary to enrich the DTOs.
        // Loading the aggregate is fine here: it's already a read path and
        // the instance fits comfortably in memory.
        var instance = await _instanceRepo.GetByIdAsync(
            query.WorkflowInstanceId, cancellationToken);

        var stepNames = instance?.Steps
            .ToDictionary(s => s.Id, s => s.StepName)
            ?? new Dictionary<Guid, string>();

        var dtos = attachments
            .Select(a =>
            {
                var stepName = a.StepInstanceId.HasValue &&
                               stepNames.TryGetValue(a.StepInstanceId.Value, out var name)
                    ? name
                    : (string?)null;

                return new WorkflowAttachmentDto(
                    Id:               a.Id,
                    FileName:         a.FileName,
                    ContentType:      a.ContentType,
                    FileSizeBytes:    a.FileSizeBytes,
                    UploadedByUserId: a.CreatedBy,
                    CreatedAt:        a.CreatedAt,
                    IsImage:          a.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase),
                    StepInstanceId:   a.StepInstanceId,
                    StepName:         stepName);
            })
            .ToList();

        return Result.Success<IReadOnlyList<WorkflowAttachmentDto>>(dtos);
    }
}
