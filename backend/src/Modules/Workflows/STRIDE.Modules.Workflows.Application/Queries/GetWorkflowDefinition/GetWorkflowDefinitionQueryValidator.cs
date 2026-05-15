using FluentValidation;

namespace STRIDE.Modules.Workflows.Application.Queries.GetWorkflowDefinition;

public sealed class GetWorkflowDefinitionQueryValidator : AbstractValidator<GetWorkflowDefinitionQuery>
{
    public GetWorkflowDefinitionQueryValidator()
    {
        RuleFor(x => x.WorkflowDefinitionId)
            .NotEmpty().WithMessage("WorkflowDefinitionId is required.");
    }
}
