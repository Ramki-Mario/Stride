using FluentValidation;

namespace STRIDE.Modules.Notifications.Application.Queries.GetNotifications;

public sealed class GetNotificationsQueryValidator : AbstractValidator<GetNotificationsQuery>
{
    public GetNotificationsQueryValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");

        RuleFor(x => x.RecipientId)
            .NotEmpty().WithMessage("RecipientId is required.");
    }
}
