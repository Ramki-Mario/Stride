using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Application.Commands.FailStep;

internal sealed class FailStepCommandHandler
    : IRequestHandler<FailStepCommand, Result>
{
    private readonly IWorkflowInstanceRepository _instances;
    private readonly ILogger<FailStepCommandHandler> _logger;

    public FailStepCommandHandler(
        IWorkflowInstanceRepository instances,
        ILogger<FailStepCommandHandler> logger)
    {
        _instances = instances;
        _logger    = logger;
    }

    public async Task<Result> Handle(FailStepCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var instance = await _instances.GetByIdAsync(request.WorkflowInstanceId, cancellationToken);
            if (instance is null)
                return Result.Failure($"Workflow instance '{request.WorkflowInstanceId}' not found.");

            instance.FailStep(request.StepInstanceId, request.Reason, request.FailedBy);
            _instances.Update(instance);
            await _instances.SaveChangesAsync(cancellationToken);

            _logger.LogWarning(
                "Step {StepId} in instance {InstanceId} failed by {UserId}: {Reason}",
                request.StepInstanceId, instance.Id, request.FailedBy, request.Reason);

            return Result.Success();
        }
        catch (WorkflowDomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violated during FailStep");
            return Result.Failure(ex.Message);
        }
    }
}
