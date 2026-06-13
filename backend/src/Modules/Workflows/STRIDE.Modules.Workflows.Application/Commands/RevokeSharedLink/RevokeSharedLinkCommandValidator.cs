using FluentValidation;

namespace STRIDE.Modules.Workflows.Application.Commands.RevokeSharedLink;

public sealed class RevokeSharedLinkCommandValidator : AbstractValidator<RevokeSharedLinkCommand>
{
    public RevokeSharedLinkCommandValidator()
    {
        RuleFor(x => x.WorkflowInstanceId)
            .NotEmpty().WithMessage("WorkflowInstanceId is required.");

        RuleFor(x => x.LinkId)
            .NotEmpty().WithMessage("LinkId is required.");
    }
}
