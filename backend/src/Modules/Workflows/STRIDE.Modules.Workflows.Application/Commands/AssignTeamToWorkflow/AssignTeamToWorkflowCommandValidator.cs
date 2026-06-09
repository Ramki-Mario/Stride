using FluentValidation;

namespace STRIDE.Modules.Workflows.Application.Commands.AssignTeamToWorkflow;

internal sealed class AssignTeamToWorkflowCommandValidator
    : AbstractValidator<AssignTeamToWorkflowCommand>
{
    public AssignTeamToWorkflowCommandValidator()
    {
        RuleFor(x => x.WorkflowInstanceId).NotEmpty();
        RuleFor(x => x.AssignedBy).NotEmpty();
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
