using FluentValidation;

namespace STRIDE.Modules.Workflows.Application.Commands.AssignStep;

public sealed class AssignStepCommandValidator : AbstractValidator<AssignStepCommand>
{
    public AssignStepCommandValidator()
    {
        RuleFor(x => x.WorkflowInstanceId)
            .NotEmpty().WithMessage("WorkflowInstanceId is required.");

        RuleFor(x => x.StepInstanceId)
            .NotEmpty().WithMessage("StepInstanceId is required.");

        RuleFor(x => x.AssigneeId)
            .NotEmpty().WithMessage("AssigneeId is required.");

        RuleFor(x => x.AssignedBy)
            .NotEmpty().WithMessage("AssignedBy (user ID) is required.");
    }
}
