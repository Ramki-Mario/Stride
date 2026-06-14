using FluentValidation;
using CronosExpression = Cronos.CronExpression;

namespace STRIDE.Modules.Scheduling.Application.Commands.CreateSchedule;

public sealed class CreateScheduleCommandValidator : AbstractValidator<CreateScheduleCommand>
{
    public CreateScheduleCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Schedule name is required.")
            .MaximumLength(100).WithMessage("Schedule name cannot exceed 100 characters.");

        RuleFor(x => x.WorkflowDefinitionId)
            .NotEmpty().WithMessage("A workflow definition must be selected.");

        RuleFor(x => x.CronExpression)
            .NotEmpty().WithMessage("Cron expression is required.")
            .Must(BeValidCron).WithMessage("Cron expression is invalid. Use standard 5-field format (e.g. '0 9 * * 1').");
    }

    private static bool BeValidCron(string expression)
    {
        try
        {
            CronosExpression.Parse(expression, Cronos.CronFormat.Standard);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
