using FluentValidation;

namespace STRIDE.Modules.Administration.Application.Commands.UpdateTenantSettings;

public sealed class UpdateTenantSettingsCommandValidator
    : AbstractValidator<UpdateTenantSettingsCommand>
{
    private static readonly string[] AllowedPalettes = ["indigo", "purple"];

    public UpdateTenantSettingsCommandValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.UpdatedBy).NotEmpty();

        RuleFor(x => x.DisplayName)
            .MaximumLength(200)
            .When(x => x.DisplayName is not null);

        RuleFor(x => x.DefaultPalette)
            .NotEmpty()
            .Must(p => AllowedPalettes.Contains(p))
            .WithMessage("DefaultPalette must be one of: indigo, purple.");

        RuleFor(x => x.Timezone)
            .NotEmpty()
            .MaximumLength(100);
    }
}
