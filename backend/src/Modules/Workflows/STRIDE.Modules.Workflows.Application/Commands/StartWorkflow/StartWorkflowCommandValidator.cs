using FluentValidation;

namespace STRIDE.Modules.Workflows.Application.Commands.StartWorkflow;

public sealed class StartWorkflowCommandValidator : AbstractValidator<StartWorkflowCommand>
{
    public StartWorkflowCommandValidator()
    {
        RuleFor(x => x.WorkflowDefinitionId)
            .NotEmpty().WithMessage("WorkflowDefinitionId is required.");

        RuleFor(x => x.StartedBy)
            .NotEmpty().WithMessage("StartedBy (user ID) is required.");
    }
}
