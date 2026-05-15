using FluentValidation;

namespace STRIDE.Modules.Workflows.Application.Commands.PauseWorkflow;

public sealed class PauseWorkflowCommandValidator : AbstractValidator<PauseWorkflowCommand>
{
    public PauseWorkflowCommandValidator()
    {
        RuleFor(x => x.WorkflowInstanceId)
            .NotEmpty().WithMessage("WorkflowInstanceId is required.");

        RuleFor(x => x.PausedBy)
            .NotEmpty().WithMessage("PausedBy (user ID) is required.");
    }
}
