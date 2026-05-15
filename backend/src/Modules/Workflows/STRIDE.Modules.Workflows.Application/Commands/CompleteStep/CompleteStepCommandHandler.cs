using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
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

    public async Task<Result> Handle(CompleteStepCommand request, CancellationToken ct)
    {
        try
        {
            var instance = await _instances.GetByIdAsync(request.WorkflowInstanceId, ct);
            if (instance is null)
                return Result.Failure($"Workflow instance '{request.WorkflowInstanceId}' not found.");

            instance.CompleteStep(request.StepInstanceId, request.CompletedBy);
            _instances.Update(instance);
            await _instances.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Step {StepId} in instance {InstanceId} completed by {UserId}",
                request.StepInstanceId, instance.Id, request.CompletedBy);

            return Result.Success();
        }
        catch (WorkflowDomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violated during CompleteStep");
            return Result.Failure(ex.Message);
        }
    }
}
