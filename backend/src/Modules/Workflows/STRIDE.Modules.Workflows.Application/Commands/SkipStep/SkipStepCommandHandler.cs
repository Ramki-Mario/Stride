using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Application.Commands.SkipStep;

internal sealed class SkipStepCommandHandler
    : IRequestHandler<SkipStepCommand, Result>
{
    private readonly IWorkflowInstanceRepository _instances;
    private readonly ILogger<SkipStepCommandHandler> _logger;

    public SkipStepCommandHandler(
        IWorkflowInstanceRepository instances,
        ILogger<SkipStepCommandHandler> logger)
    {
        _instances = instances;
        _logger    = logger;
    }

    public async Task<Result> Handle(SkipStepCommand request, CancellationToken ct)
    {
        try
        {
            var instance = await _instances.GetByIdAsync(request.WorkflowInstanceId, ct);
            if (instance is null)
                return Result.Failure($"Workflow instance '{request.WorkflowInstanceId}' not found.");

            instance.SkipStep(request.StepInstanceId, request.SkippedBy);
            _instances.Update(instance);
            await _instances.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Step {StepId} in instance {InstanceId} skipped by {UserId}",
                request.StepInstanceId, instance.Id, request.SkippedBy);

            return Result.Success();
        }
        catch (WorkflowDomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violated during SkipStep");
            return Result.Failure(ex.Message);
        }
    }
}
