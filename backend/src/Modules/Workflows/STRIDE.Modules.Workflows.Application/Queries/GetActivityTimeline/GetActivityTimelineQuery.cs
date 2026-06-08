using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Queries.GetActivityTimeline;

public sealed record GetActivityTimelineQuery(
    Guid WorkflowInstanceId,
    int  Page      = 1,
    int  PageSize  = 20,
    bool Ascending = true) : IRequest<Result<PagedActivityDto>>;
