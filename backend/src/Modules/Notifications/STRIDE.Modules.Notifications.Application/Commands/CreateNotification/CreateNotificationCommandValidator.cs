using FluentValidation;

namespace STRIDE.Modules.Notifications.Application.Commands.CreateNotification;

public sealed class CreateNotificationCommandValidator : AbstractValidator<CreateNotificationCommand>
{
    public CreateNotificationCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage("TenantId is required.");

        RuleFor(x => x.RecipientId)
            .NotEmpty().WithMessage("RecipientId is required.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Body is required.")
            .MaximumLength(1000).WithMessage("Body must not exceed 1000 characters.");

        RuleFor(x => x.CreatedBy)
            .NotEmpty().WithMessage("CreatedBy is required.");
    }
}
