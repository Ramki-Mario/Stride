namespace STRIDE.Modules.Workflows.Application.Queries.ListWorkflowComments;

public sealed record WorkflowCommentDto(
    Guid      Id,
    Guid      WorkflowInstanceId,
    Guid      AuthorId,
    string    Body,
    bool      IsDeleted,
    DateTime  CreatedAt,
    DateTime? EditedAt);

/// <summary>
/// Paginated wrapper returned by <see cref="ListWorkflowCommentsQuery"/>.
/// </summary>
public sealed record PagedCommentsDto(
    IReadOnlyList<WorkflowCommentDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
