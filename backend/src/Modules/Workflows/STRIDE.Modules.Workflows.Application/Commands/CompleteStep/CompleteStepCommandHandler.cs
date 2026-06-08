using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Application.Commands.CompleteStep;

internal sealed class CompleteStepCommandHandler
    : IRequestHandler<CompleteStepCommand, Result>
{
    private readonly IWorkflowInstanceRepository _instances;
    private readonly ILogger<CompleteStepCommandHandler> _logger;

    public CompleteStepCommandHandler(
        IWorkflowInstanceRepository instances,
        ILogger<CompleteStepCommandHandler> logger)
    {
        _instances = instances;
        _logger    = logger;
    }

    public async Task<Result> Handle(CompleteStepCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var instance = await _instances.GetByIdAsync(request.WorkflowInstanceId, cancellationToken);
            if (instance is null)
                return Result.Failure($"Workflow instance '{request.WorkflowInstanceId}' not found.");

            var billableItems = request.BillableItems?
                .Select(b => (b.Description, b.Quantity, b.UnitPrice, b.Unit))
                .ToList();

            var fieldValues = request.FieldValues?
                .Select(v => (v.StepFieldDefinitionId, v.Value))
                .ToList();

            instance.CompleteStep(request.StepInstanceId, request.CompletedBy, billableItems, fieldValues);
            _instances.Update(instance);
            await _instances.SaveChangesAsync(cancellationToken);

            _logger.StepCompleted(request.StepInstanceId, instance.Id, request.CompletedBy);

            return Result.Success();
        }
        catch (WorkflowDomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violated during CompleteStep");
            return Result.Failure(ex.Message);
        }
    }
}
