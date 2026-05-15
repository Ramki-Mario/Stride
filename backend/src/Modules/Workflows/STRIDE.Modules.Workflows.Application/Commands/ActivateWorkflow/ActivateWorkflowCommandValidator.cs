using FluentValidation;

namespace STRIDE.Modules.Workflows.Application.Commands.ActivateWorkflow;

public sealed class ActivateWorkflowCommandValidator : AbstractValidator<ActivateWorkflowCommand>
{
    public ActivateWorkflowCommandValidator()
    {
        RuleFor(x => x.WorkflowDefinitionId)
            .NotEmpty().WithMessage("WorkflowDefinitionId is required.");

        RuleFor(x => x.ActivatedBy)
            .NotEmpty().WithMessage("ActivatedBy (user ID) is required.");
    }
}
