using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Application.Commands.ResumeFromHalt;

internal sealed class ResumeFromHaltCommandHandler
    : IRequestHandler<ResumeFromHaltCommand, Result>
{
    private readonly IWorkflowInstanceRepository _instances;
    private readonly ILogger<ResumeFromHaltCommandHandler> _logger;

    public ResumeFromHaltCommandHandler(
        IWorkflowInstanceRepository instances,
        ILogger<ResumeFromHaltCommandHandler> logger)
    {
        _instances = instances;
        _logger    = logger;
    }

    public async Task<Result> Handle(ResumeFromHaltCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var instance = await _instances.GetByIdAsync(request.WorkflowInstanceId, cancellationToken);
            if (instance is null)
                return Result.Failure($"Workflow instance '{request.WorkflowInstanceId}' not found.");

            instance.ResumeFromHalt(request.ResumedBy);
            _instances.Update(instance);
            await _instances.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Halted workflow instance {InstanceId} resumed by user {UserId}",
                instance.Id, request.ResumedBy);

            return Result.Success();
        }
        catch (WorkflowDomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violated during ResumeFromHalt");
            return Result.Failure(ex.Message);
        }
    }
}
