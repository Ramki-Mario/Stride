using FluentValidation;

namespace STRIDE.Modules.Workflows.Application.Commands.CompleteStep;

public sealed class CompleteStepCommandValidator : AbstractValidator<CompleteStepCommand>
{
    public CompleteStepCommandValidator()
    {
        RuleFor(x => x.WorkflowInstanceId)
            .NotEmpty().WithMessage("WorkflowInstanceId is required.");

        RuleFor(x => x.StepInstanceId)
            .NotEmpty().WithMessage("StepInstanceId is required.");

        RuleFor(x => x.CompletedBy)
            .NotEmpty().WithMessage("CompletedBy (user ID) is required.");

        RuleForEach(x => x.FieldValues)
            .ChildRules(v =>
            {
                v.RuleFor(x => x.StepFieldDefinitionId)
                    .NotEmpty().WithMessage("StepFieldDefinitionId is required.");
            });
    }
}
