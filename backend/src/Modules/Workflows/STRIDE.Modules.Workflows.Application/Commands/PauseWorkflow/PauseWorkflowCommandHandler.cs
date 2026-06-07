using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Application.Commands.PauseWorkflow;

internal sealed class PauseWorkflowCommandHandler
    : IRequestHandler<PauseWorkflowCommand, Result>
{
    private readonly IWorkflowInstanceRepository _instances;
    private readonly ILogger<PauseWorkflowCommandHandler> _logger;

    public PauseWorkflowCommandHandler(
        IWorkflowInstanceRepository instances,
        ILogger<PauseWorkflowCommandHandler> logger)
    {
        _instances = instances;
        _logger    = logger;
    }

    public async Task<Result> Handle(PauseWorkflowCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var instance = await _instances.GetByIdAsync(request.WorkflowInstanceId, cancellationToken);
            if (instance is null)
                return Result.Failure($"Workflow instance '{request.WorkflowInstanceId}' not found.");

            instance.Pause(request.PausedBy);
            _instances.Update(instance);
            await _instances.SaveChangesAsync(cancellationToken);

            _logger.WorkflowInstancePaused(instance.Id, request.PausedBy);

            return Result.Success();
        }
        catch (WorkflowDomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violated during PauseWorkflow");
            return Result.Failure(ex.Message);
        }
    }
}
