using FluentValidation;

namespace STRIDE.Modules.Workflows.Application.Commands.DeleteWorkflow;

public sealed class DeleteWorkflowCommandValidator : AbstractValidator<DeleteWorkflowCommand>
{
    public DeleteWorkflowCommandValidator()
    {
        RuleFor(x => x.WorkflowDefinitionId)
            .NotEmpty().WithMessage("WorkflowDefinitionId is required.");

        RuleFor(x => x.DeletedBy)
            .NotEmpty().WithMessage("DeletedBy (user ID) is required.");
    }
}
