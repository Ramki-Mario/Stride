using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Application.Commands.ClaimStep;

/// <summary>
/// Handles <see cref="ClaimStepCommand"/> — a user self-assigns a Pending step.
/// Role validation mirrors <c>AssignStepCommandHandler</c>; the claimant must hold the
/// step's <c>RequiredRoleId</c> (if set) before the assignment is allowed.
/// </summary>
internal sealed class ClaimStepCommandHandler
    : IRequestHandler<ClaimStepCommand, Result>
{
    private readonly IWorkflowInstanceRepository _instances;
    private readonly IUserRoleService            _userRoles;
    private readonly ILogger<ClaimStepCommandHandler> _logger;

    public ClaimStepCommandHandler(
        IWorkflowInstanceRepository instances,
        IUserRoleService            userRoles,
        ILogger<ClaimStepCommandHandler> logger)
    {
        _instances = instances;
        _userRoles = userRoles;
        _logger    = logger;
    }

    public async Task<Result> Handle(ClaimStepCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var instance = await _instances.GetByIdAsync(request.WorkflowInstanceId, cancellationToken);
            if (instance is null)
                return Result.Failure($"Workflow instance '{request.WorkflowInstanceId}' not found.");

            var step = instance.Steps.FirstOrDefault(s => s.Id == request.StepInstanceId);
            if (step is null)
                return Result.Failure($"Step '{request.StepInstanceId}' not found in instance '{instance.Id}'.");

            // Role guard — step can restrict who is allowed to claim it.
            if (step.RequiredRoleId.HasValue)
            {
                var hasRole = await _userRoles.UserHasRoleAsync(
                    request.ClaimantId, step.RequiredRoleId.Value, instance.TenantId, cancellationToken);

                if (!hasRole)
                    return Result.Failure(
                        $"You do not hold the required role to claim step '{step.StepName}'.");
            }

            // Self-assign: claimant is both the assignee and the acting user.
            instance.AssignStep(request.StepInstanceId, request.ClaimantId, request.ClaimantId);
            _instances.Update(instance);
            await _instances.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Step {StepId} in instance {InstanceId} claimed by user {ClaimantId}",
                request.StepInstanceId, instance.Id, request.ClaimantId);

            return Result.Success();
        }
        catch (WorkflowDomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violated during ClaimStep");
            return Result.Failure(ex.Message);
        }
    }
}
