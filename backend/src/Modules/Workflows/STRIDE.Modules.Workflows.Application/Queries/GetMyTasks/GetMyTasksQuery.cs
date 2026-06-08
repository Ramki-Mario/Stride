using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Queries.GetMyTasks;

/// <summary>
/// Returns all non-terminal step instances assigned to <see cref="UserId"/>
/// within the current <see cref="TenantId"/>.
/// </summary>
public sealed record GetMyTasksQuery(Guid UserId, Guid TenantId)
    : IRequest<Result<IReadOnlyList<MyTaskDto>>>;
