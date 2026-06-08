using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.DTOs;

namespace STRIDE.Modules.Workflows.Application.Queries.ListWorkflowAttachments;

/// <summary>
/// Returns all non-deleted attachments for a workflow instance — both instance-level
/// attachments (StepInstanceId == null) and step-level attachments (StepInstanceId != null),
/// enriched with the originating step's name.
/// </summary>
public sealed record ListWorkflowAttachmentsQuery(Guid WorkflowInstanceId)
    : IRequest<Result<IReadOnlyList<WorkflowAttachmentDto>>>;
