using FluentValidation;
using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Application.Commands.CreateSharedLink;

public sealed class CreateSharedLinkCommandValidator : AbstractValidator<CreateSharedLinkCommand>
{
    public CreateSharedLinkCommandValidator()
    {
        RuleFor(x => x.WorkflowInstanceId)
            .NotEmpty().WithMessage("WorkflowInstanceId is required.");

        RuleFor(x => x.CreatedByUserId)
            .NotEmpty().WithMessage("CreatedByUserId is required.");

        // Optional override; when supplied it must be a sane lifetime (1 day – 1 year).
        When(x => x.ExpiryDays.HasValue, () =>
        {
            RuleFor(x => x.ExpiryDays!.Value)
                .InclusiveBetween(1, 365)
                .WithMessage("Expiry must be between 1 and 365 days.");
        });
    }
}
