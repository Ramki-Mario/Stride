using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Application.Commands.ResumeWorkflow;

internal sealed class ResumeWorkflowCommandHandler
    : IRequestHandler<ResumeWorkflowCommand, Result>
{
    private readonly IWorkflowInstanceRepository _instances;
    private readonly ILogger<ResumeWorkflowCommandHandler> _logger;

    public ResumeWorkflowCommandHandler(
        IWorkflowInstanceRepository instances,
        ILogger<ResumeWorkflowCommandHandler> logger)
    {
        _instances = instances;
        _logger    = logger;
    }

    public async Task<Result> Handle(ResumeWorkflowCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var instance = await _instances.GetByIdAsync(request.WorkflowInstanceId, cancellationToken);
            if (instance is null)
                return Result.Failure($"Workflow instance '{request.WorkflowInstanceId}' not found.");

            instance.Resume(request.ResumedBy);
            _instances.Update(instance);
            await _instances.SaveChangesAsync(cancellationToken);

            _logger.WorkflowInstanceResumed(instance.Id, request.ResumedBy);

            return Result.Success();
        }
        catch (WorkflowDomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violated during ResumeWorkflow");
            return Result.Failure(ex.Message);
        }
    }
}
