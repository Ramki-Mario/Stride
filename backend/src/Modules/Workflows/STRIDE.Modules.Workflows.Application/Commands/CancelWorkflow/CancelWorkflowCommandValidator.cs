using FluentValidation;

namespace STRIDE.Modules.Workflows.Application.Commands.CancelWorkflow;

public sealed class CancelWorkflowCommandValidator : AbstractValidator<CancelWorkflowCommand>
{
    public CancelWorkflowCommandValidator()
    {
        RuleFor(x => x.WorkflowInstanceId)
            .NotEmpty().WithMessage("WorkflowInstanceId is required.");

        RuleFor(x => x.CancelledBy)
            .NotEmpty().WithMessage("CancelledBy (user ID) is required.");
    }
}
