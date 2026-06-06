using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.Abstractions;

namespace STRIDE.Modules.Workflows.Application.Queries.GetWorkflowInstance;

internal sealed class GetWorkflowInstanceQueryHandler
    : IRequestHandler<GetWorkflowInstanceQuery, Result<WorkflowInstanceDto>>
{
    private readonly IWorkflowInstanceRepository _instances;
    private readonly ILogger<GetWorkflowInstanceQueryHandler> _logger;

    public GetWorkflowInstanceQueryHandler(
        IWorkflowInstanceRepository instances,
        ILogger<GetWorkflowInstanceQueryHandler> logger)
    {
        _instances = instances;
        _logger    = logger;
    }

    public async Task<Result<WorkflowInstanceDto>> Handle(
        GetWorkflowInstanceQuery request,
        CancellationToken cancellationToken)
    {
        var instance = await _instances.GetByIdAsync(request.WorkflowInstanceId, cancellationToken);
        if (instance is null)
        {
            _logger.LogWarning(
                "GetWorkflowInstance: instance {Id} not found", request.WorkflowInstanceId);
            return Result.Failure<WorkflowInstanceDto>(
                $"Workflow instance '{request.WorkflowInstanceId}' not found.");
        }

        var dto = new WorkflowInstanceDto(
            instance.Id,
            instance.TenantId,
            instance.WorkflowDefinitionId,
            instance.WorkflowName,
            instance.Status,
            instance.StartedBy,
            instance.CreatedAt,
            instance.UpdatedAt,
            instance.CompletedAt,
            instance.Steps
                .OrderBy(s => s.Order)
                .Select(s => new StepInstanceDto(
                    s.Id,
                    s.StepDefinitionId,
                    s.StepName,
                    s.Order,
                    s.IsRequired,
                    s.Status,
                    s.AssigneeId,
                    s.FailureReason,
                    s.CompletedAt))
                .ToList()
                .AsReadOnly());

        return Result.Success(dto);
    }
}
