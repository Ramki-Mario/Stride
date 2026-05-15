using FluentValidation;

namespace STRIDE.Modules.Workflows.Application.Commands.UpdateWorkflow;

public sealed class UpdateWorkflowCommandValidator : AbstractValidator<UpdateWorkflowCommand>
{
    public UpdateWorkflowCommandValidator()
    {
        RuleFor(x => x.WorkflowDefinitionId)
            .NotEmpty().WithMessage("WorkflowDefinitionId is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Workflow name is required.")
            .MaximumLength(200).WithMessage("Workflow name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => x.Description is not null);

        RuleFor(x => x.UpdatedBy)
            .NotEmpty().WithMessage("UpdatedBy (user ID) is required.");
    }
}
