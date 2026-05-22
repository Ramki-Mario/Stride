using FluentValidation;

namespace STRIDE.Modules.Notifications.Application.Queries.GetUnreadCount;

public sealed class GetUnreadCountQueryValidator : AbstractValidator<GetUnreadCountQuery>
{
    public GetUnreadCountQueryValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");

        RuleFor(x => x.RecipientId)
            .NotEmpty().WithMessage("RecipientId is required.");
    }
}
