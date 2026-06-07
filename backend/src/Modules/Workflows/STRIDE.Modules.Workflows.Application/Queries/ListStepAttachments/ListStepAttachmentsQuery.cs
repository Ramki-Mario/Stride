using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.DTOs;

namespace STRIDE.Modules.Workflows.Application.Queries.ListStepAttachments;

/// <summary>Returns all non-deleted attachments for a specific step instance.</summary>
public sealed record ListStepAttachmentsQuery(
    Guid StepInstanceId) : IRequest<Result<IReadOnlyList<AttachmentDto>>>;
