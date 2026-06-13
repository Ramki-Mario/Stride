using FluentValidation;
using STRIDE.Modules.Webhooks.Domain;

namespace STRIDE.Modules.Webhooks.Application.Commands.CreateWebhookSubscription;

public sealed class CreateWebhookSubscriptionCommandValidator : AbstractValidator<CreateWebhookSubscriptionCommand>
{
    public CreateWebhookSubscriptionCommandValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("Webhook URL is required.")
            .MaximumLength(2048).WithMessage("Webhook URL must not exceed 2048 characters.")
            .Must(BeHttpsAbsoluteUrl).WithMessage("Webhook URL must be a valid absolute HTTPS URL.");

        RuleFor(x => x.EventTypes)
            .NotEmpty().WithMessage("At least one event type must be selected.");

        RuleForEach(x => x.EventTypes)
            .Must(WebhookEventTypes.IsKnown).WithMessage("One or more selected event types are not recognised.");
    }

    private static bool BeHttpsAbsoluteUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
}
