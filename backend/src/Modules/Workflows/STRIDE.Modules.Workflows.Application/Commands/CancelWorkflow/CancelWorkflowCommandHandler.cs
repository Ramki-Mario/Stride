using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Application.Commands.CancelWorkflow;

internal sealed class CancelWorkflowCommandHandler
    : IRequestHandler<CancelWorkflowCommand, Result>
{
    private readonly IWorkflowInstanceRepository _instances;
    private readonly ILogger<CancelWorkflowCommandHandler> _logger;

    public CancelWorkflowCommandHandler(
        IWorkflowInstanceRepository instances,
        ILogger<CancelWorkflowCommandHandler> logger)
    {
        _instances = instances;
        _logger    = logger;
    }

    public async Task<Result> Handle(CancelWorkflowCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var instance = await _instances.GetByIdAsync(request.WorkflowInstanceId, cancellationToken);
            if (instance is null)
                return Result.Failure($"Workflow instance '{request.WorkflowInstanceId}' not found.");

            instance.Cancel(request.CancelledBy);
            _instances.Update(instance);
            await _instances.SaveChangesAsync(cancellationToken);

            _logger.WorkflowInstanceCancelled(instance.Id, request.CancelledBy);

            return Result.Success();
        }
        catch (WorkflowDomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violated during CancelWorkflow");
            return Result.Failure(ex.Message);
        }
    }
}
