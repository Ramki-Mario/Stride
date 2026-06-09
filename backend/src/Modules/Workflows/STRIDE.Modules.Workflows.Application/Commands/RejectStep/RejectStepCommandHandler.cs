using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Application.Commands.RejectStep;

internal sealed class RejectStepCommandHandler
    : IRequestHandler<RejectStepCommand, Result>
{
    private readonly IWorkflowInstanceRepository _instances;
    private readonly IUserRoleService            _userRoles;
    private readonly ILogger<RejectStepCommandHandler> _logger;

    public RejectStepCommandHandler(
        IWorkflowInstanceRepository instances,
        IUserRoleService            userRoles,
        ILogger<RejectStepCommandHandler> logger)
    {
        _instances = instances;
        _userRoles = userRoles;
        _logger    = logger;
    }

    public async Task<Result> Handle(RejectStepCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var instance = await _instances.GetByIdAsync(request.WorkflowInstanceId, cancellationToken);
            if (instance is null)
                return Result.Failure($"Workflow instance '{request.WorkflowInstanceId}' not found.");

            var step = instance.Steps.FirstOrDefault(s => s.Id == request.StepInstanceId);
            if (step is null)
                return Result.Failure($"Step '{request.StepInstanceId}' not found in instance '{instance.Id}'.");

            // Role guard — only users who hold the required role may reject.
            if (step.RequiredRoleId.HasValue)
            {
                var hasRole = await _userRoles.UserHasRoleAsync(
                    request.RejectedBy, step.RequiredRoleId.Value, instance.TenantId, cancellationToken);

                if (!hasRole)
                    return Result.Failure(
                        $"You do not hold the required role to reject step '{step.StepName}'.");
            }

            instance.RejectStep(request.StepInstanceId, request.RejectedBy, request.Comment);
            _instances.Update(instance);
            await _instances.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Step {StepId} in instance {InstanceId} rejected by user {UserId}",
                request.StepInstanceId, instance.Id, request.RejectedBy);

            return Result.Success();
        }
        catch (WorkflowDomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violated during RejectStep");
            return Result.Failure(ex.Message);
        }
    }
}
