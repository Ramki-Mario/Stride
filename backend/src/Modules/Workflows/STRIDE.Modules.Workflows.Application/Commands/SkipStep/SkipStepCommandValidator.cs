using FluentValidation;

namespace STRIDE.Modules.Workflows.Application.Commands.SkipStep;

public sealed class SkipStepCommandValidator : AbstractValidator<SkipStepCommand>
{
    public SkipStepCommandValidator()
    {
        RuleFor(x => x.WorkflowInstanceId)
            .NotEmpty().WithMessage("WorkflowInstanceId is required.");

        RuleFor(x => x.StepInstanceId)
            .NotEmpty().WithMessage("StepInstanceId is required.");

        RuleFor(x => x.SkippedBy)
            .NotEmpty().WithMessage("SkippedBy (user ID) is required.");
    }
}
