using FluentValidation;

namespace STRIDE.Modules.Workflows.Application.Commands.ResumeFromHalt;

public sealed class ResumeFromHaltCommandValidator : AbstractValidator<ResumeFromHaltCommand>
{
    public ResumeFromHaltCommandValidator()
    {
        RuleFor(x => x.WorkflowInstanceId)
            .NotEmpty().WithMessage("WorkflowInstanceId is required.");

        RuleFor(x => x.ResumedBy)
            .NotEmpty().WithMessage("ResumedBy (user ID) is required.");
    }
}
