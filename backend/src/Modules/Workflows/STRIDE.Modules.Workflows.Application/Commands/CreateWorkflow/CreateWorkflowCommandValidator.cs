using FluentValidation;

namespace STRIDE.Modules.Workflows.Application.Commands.CreateWorkflow;

public sealed class CreateWorkflowCommandValidator : AbstractValidator<CreateWorkflowCommand>
{
    public CreateWorkflowCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Workflow name is required.")
            .MaximumLength(200).WithMessage("Workflow name must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => x.Description is not null);

        RuleFor(x => x.CreatedBy)
            .NotEmpty().WithMessage("CreatedBy (user ID) is required.");

        RuleFor(x => x.Steps)
            .NotNull().WithMessage("Steps list is required (may be empty).");

        RuleForEach(x => x.Steps).ChildRules(step =>
        {
            step.RuleFor(s => s.Name)
                .NotEmpty().WithMessage("Step name is required.")
                .MaximumLength(200).WithMessage("Step name must not exceed 200 characters.");

            step.RuleFor(s => s.Description)
                .MaximumLength(1000).WithMessage("Step description must not exceed 1000 characters.")
                .When(s => s.Description is not null);
        });
    }
}
