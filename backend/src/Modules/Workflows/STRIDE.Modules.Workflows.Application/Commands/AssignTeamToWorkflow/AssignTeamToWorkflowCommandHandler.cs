using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.Abstractions;

namespace STRIDE.Modules.Workflows.Application.Commands.AssignTeamToWorkflow;

internal sealed class AssignTeamToWorkflowCommandHandler
    : IRequestHandler<AssignTeamToWorkflowCommand, Result>
{
    private readonly IWorkflowInstanceRepository              _instances;
    private readonly IAuditLogger                             _audit;
    private readonly ICurrentUser                             _currentUser;
    private readonly ILogger<AssignTeamToWorkflowCommandHandler> _logger;

    public AssignTeamToWorkflowCommandHandler(
        IWorkflowInstanceRepository instances,
        IAuditLogger                audit,
        ICurrentUser                currentUser,
        ILogger<AssignTeamToWorkflowCommandHandler> logger)
    {
        _instances   = instances;
        _audit       = audit;
        _currentUser = currentUser;
        _logger      = logger;
    }

    public async Task<Result> Handle(
        AssignTeamToWorkflowCommand request,
        CancellationToken cancellationToken)
    {
        var instance = await _instances.GetByIdAsync(request.WorkflowInstanceId, cancellationToken);
        if (instance is null)
            return Result.Failure($"Workflow instance '{request.WorkflowInstanceId}' not found.");

        instance.AssignTeam(request.TeamId, request.AssignedBy);
        _instances.Update(instance);
        await _instances.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Team {TeamId} assigned to workflow instance {InstanceId} by {AssignedBy}",
            request.TeamId, instance.Id, request.AssignedBy);

        _ = _audit.LogAsync(new AuditLogEntry(
            TenantId:     request.TenantId,
            ActorId:      request.AssignedBy,
            ActorEmail:   _currentUser.Email,
            Action:       AuditActions.WorkflowTeamAssigned,
            ResourceType: "WorkflowInstance",
            ResourceId:   instance.Id,
            NewValueJson: $"{{\"teamId\":{(request.TeamId.HasValue ? $"\"{request.TeamId}\"" : "null")}}}"));

        return Result.Success();
    }
}
