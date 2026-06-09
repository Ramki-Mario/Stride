using FluentValidation;

namespace STRIDE.Modules.Workflows.Application.Commands.ApproveStep;

public sealed class ApproveStepCommandValidator : AbstractValidator<ApproveStepCommand>
{
    public ApproveStepCommandValidator()
    {
        RuleFor(x => x.WorkflowInstanceId)
            .NotEmpty().WithMessage("WorkflowInstanceId is required.");

        RuleFor(x => x.StepInstanceId)
            .NotEmpty().WithMessage("StepInstanceId is required.");

        RuleFor(x => x.ApprovedBy)
            .NotEmpty().WithMessage("ApprovedBy (user ID) is required.");

        RuleFor(x => x.Comment)
            .NotEmpty().WithMessage("A comment explaining your decision is required.")
            .MinimumLength(10).WithMessage("Comment must be at least 10 characters.")
            .MaximumLength(1000).WithMessage("Comment must not exceed 1000 characters.");
    }
}
