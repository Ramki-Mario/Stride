using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.Abstractions;

namespace STRIDE.Modules.Workflows.Application.Queries.ListWorkflowComments;

internal sealed class ListWorkflowCommentsQueryHandler
    : IRequestHandler<ListWorkflowCommentsQuery, Result<PagedCommentsDto>>
{
    private readonly IWorkflowCommentRepository _comments;

    public ListWorkflowCommentsQueryHandler(IWorkflowCommentRepository comments)
        => _comments = comments;

    public async Task<Result<PagedCommentsDto>> Handle(
        ListWorkflowCommentsQuery request,
        CancellationToken cancellationToken)
    {
        var page     = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var (items, totalCount) = await _comments.ListByInstanceAsync(
            request.WorkflowInstanceId, page, pageSize, cancellationToken);

        var dtos = items
            .Select(c => new WorkflowCommentDto(
                c.Id,
                c.WorkflowInstanceId,
                c.CreatedBy,
                c.Body,
                c.IsDeleted,
                c.CreatedAt,
                c.EditedAt))
            .ToList()
            .AsReadOnly();

        return Result.Success(new PagedCommentsDto(dtos, totalCount, page, pageSize));
    }
}
