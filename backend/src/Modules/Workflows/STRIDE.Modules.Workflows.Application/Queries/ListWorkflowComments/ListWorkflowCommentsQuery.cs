using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Queries.ListWorkflowComments;

public sealed record ListWorkflowCommentsQuery(
    Guid WorkflowInstanceId,
    int  Page     = 1,
    int  PageSize = 20) : IRequest<Result<PagedCommentsDto>>;
