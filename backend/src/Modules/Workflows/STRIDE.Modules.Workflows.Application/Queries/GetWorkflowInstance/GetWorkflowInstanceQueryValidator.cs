using FluentValidation;

namespace STRIDE.Modules.Workflows.Application.Queries.GetWorkflowInstance;

public sealed class GetWorkflowInstanceQueryValidator : AbstractValidator<GetWorkflowInstanceQuery>
{
    public GetWorkflowInstanceQueryValidator()
    {
        RuleFor(x => x.WorkflowInstanceId)
            .NotEmpty().WithMessage("WorkflowInstanceId is required.");
    }
}
