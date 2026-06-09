using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.Abstractions;

namespace STRIDE.Modules.Workflows.Application.Queries.GetMyTasks;

internal sealed class GetMyTasksQueryHandler
    : IRequestHandler<GetMyTasksQuery, Result<IReadOnlyList<MyTaskDto>>>
{
    private readonly IWorkflowReadService _readService;

    public GetMyTasksQueryHandler(IWorkflowReadService readService)
        => _readService = readService;

    public async Task<Result<IReadOnlyList<MyTaskDto>>> Handle(
        GetMyTasksQuery request,
        CancellationToken cancellationToken)
    {
        var tasks = await _readService.GetMyTasksAsync(
            request.UserId,
            request.TenantId,
            cancellationToken);

        var dtos = tasks
            .Select(t => new MyTaskDto(
                t.StepInstanceId,
                t.StepName,
                t.StepStatus,
                t.AssignedAt,
                t.WorkflowInstanceId,
                t.WorkflowName,
                t.WorkflowStatus,
                t.ClientName,
                t.IsApprovalTask))
            .ToList()
            .AsReadOnly();

        return Result.Success<IReadOnlyList<MyTaskDto>>(dtos);
    }
}
