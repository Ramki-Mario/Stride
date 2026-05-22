using FluentValidation;
using STRIDE.Modules.Identity.Application.Abstractions;

namespace STRIDE.Modules.Identity.Application.Commands.RegisterTenant;

internal sealed class RegisterTenantCommandValidator : AbstractValidator<RegisterTenantCommand>
{
    private static readonly string[] ValidPlans = ["Starter", "Pro", "Enterprise"];

    public RegisterTenantCommandValidator(ITenantRepository tenants)
    {
        RuleFor(x => x.OrgName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Slug)
            .NotEmpty()
            .MaximumLength(100)
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
                .WithMessage("Slug must be lowercase letters, numbers, and hyphens only (e.g. 'acme-corp').")
            .MustAsync(async (slug, ct) => !await tenants.ExistsBySlugAsync(slug, ct))
                .WithMessage("This organisation slug is already taken.");

        RuleFor(x => x.Plan)
            .NotEmpty()
            .Must(p => ValidPlans.Contains(p))
                .WithMessage("Plan must be Starter, Pro, or Enterprise.");

        RuleFor(x => x.AdminEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.AdminPassword)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(256)
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one number.");

        RuleFor(x => x.AdminDisplayName)
            .NotEmpty()
            .MaximumLength(100);
    }
}
