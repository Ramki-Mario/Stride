using FluentValidation;

namespace STRIDE.Modules.Workflows.Application.Commands.FailStep;

public sealed class FailStepCommandValidator : AbstractValidator<FailStepCommand>
{
    public FailStepCommandValidator()
    {
        RuleFor(x => x.WorkflowInstanceId)
            .NotEmpty().WithMessage("WorkflowInstanceId is required.");

        RuleFor(x => x.StepInstanceId)
            .NotEmpty().WithMessage("StepInstanceId is required.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A failure reason is required.")
            .MaximumLength(2000).WithMessage("Reason must not exceed 2000 characters.");

        RuleFor(x => x.FailedBy)
            .NotEmpty().WithMessage("FailedBy (user ID) is required.");
    }
}
