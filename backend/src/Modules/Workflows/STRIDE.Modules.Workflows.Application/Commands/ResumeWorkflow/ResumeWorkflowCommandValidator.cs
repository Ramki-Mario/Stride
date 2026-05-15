using FluentValidation;

namespace STRIDE.Modules.Workflows.Application.Commands.ResumeWorkflow;

public sealed class ResumeWorkflowCommandValidator : AbstractValidator<ResumeWorkflowCommand>
{
    public ResumeWorkflowCommandValidator()
    {
        RuleFor(x => x.WorkflowInstanceId)
            .NotEmpty().WithMessage("WorkflowInstanceId is required.");

        RuleFor(x => x.ResumedBy)
            .NotEmpty().WithMessage("ResumedBy (user ID) is required.");
    }
}
