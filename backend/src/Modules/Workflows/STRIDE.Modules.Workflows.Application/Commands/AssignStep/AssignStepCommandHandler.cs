using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Application.Commands.AssignStep;

internal sealed class AssignStepCommandHandler
    : IRequestHandler<AssignStepCommand, Result>
{
    private readonly IWorkflowInstanceRepository _instances;
    private readonly ILogger<AssignStepCommandHandler> _logger;

    public AssignStepCommandHandler(
        IWorkflowInstanceRepository instances,
        ILogger<AssignStepCommandHandler> logger)
    {
        _instances = instances;
        _logger    = logger;
    }

    public async Task<Result> Handle(AssignStepCommand request, CancellationToken ct)
    {
        try
        {
            var instance = await _instances.GetByIdAsync(request.WorkflowInstanceId, ct);
            if (instance is null)
                return Result.Failure($"Workflow instance '{request.WorkflowInstanceId}' not found.");

            instance.AssignStep(request.StepInstanceId, request.AssigneeId, request.AssignedBy);
            _instances.Update(instance);
            await _instances.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Step {StepId} in instance {InstanceId} assigned to {AssigneeId} by {AssignedBy}",
                request.StepInstanceId, instance.Id, request.AssigneeId, request.AssignedBy);

            return Result.Success();
        }
        catch (WorkflowDomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violated during AssignStep");
            return Result.Failure(ex.Message);
        }
    }
}
