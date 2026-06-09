using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.Helpers;

namespace STRIDE.Modules.Workflows.Application.Queries.GetWorkflowInstance;

internal sealed class GetWorkflowInstanceQueryHandler
    : IRequestHandler<GetWorkflowInstanceQuery, Result<WorkflowInstanceDto>>
{
    private readonly IWorkflowInstanceRepository _instances;
    private readonly IWorkflowDefinitionRepository _definitions;
    private readonly ILogger<GetWorkflowInstanceQueryHandler> _logger;

    public GetWorkflowInstanceQueryHandler(
        IWorkflowInstanceRepository instances,
        IWorkflowDefinitionRepository definitions,
        ILogger<GetWorkflowInstanceQueryHandler> logger)
    {
        _instances   = instances;
        _definitions = definitions;
        _logger      = logger;
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

        var definition = await _definitions.GetByIdAsync(instance.WorkflowDefinitionId, cancellationToken);
        var fieldsByStepDefinitionId = definition?.Steps
            .ToDictionary(
                s => s.Id,
                s => (IReadOnlyList<StepFieldDefinitionDto>)s.Fields
                    .OrderBy(f => f.DisplayOrder)
                    .Select(f => new StepFieldDefinitionDto(
                        f.Id,
                        f.Label,
                        f.FieldType.ToString(),
                        f.IsRequired,
                        f.DisplayOrder,
                        f.HelpText,
                        f.DropdownOptions))
                    .ToList()
                    .AsReadOnly())
            ?? new Dictionary<Guid, IReadOnlyList<StepFieldDefinitionDto>>();

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
            instance.DeadlineAt,
            SlaStatusComputer.Compute(instance.CreatedAt, instance.DeadlineAt, instance.CompletedAt),
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
                    s.AssignedAt,
                    s.RequiredRoleId,
                    s.FailureReason,
                    s.CompletedAt,
                    s.DueAt,
                    s.IsOverdue,
                    s.BillableItems
                        .Select(b => new BillableItemDto(
                            b.Id,
                            b.Description,
                            b.Quantity,
                            b.UnitPrice,
                            b.Unit.ToString(),
                            b.LineTotal))
                        .ToList()
                        .AsReadOnly(),
                    fieldsByStepDefinitionId.TryGetValue(s.StepDefinitionId, out var fields)
                        ? fields
                        : Array.Empty<StepFieldDefinitionDto>(),
                    s.FieldValues
                        .Select(v => new StepFieldValueDto(
                            v.Id,
                            v.StepFieldDefinitionId,
                            v.Value))
                        .ToList()
                        .AsReadOnly()))
                .ToList()
                .AsReadOnly(),
            TeamId: instance.TeamId);

        return Result.Success(dto);
    }
}
