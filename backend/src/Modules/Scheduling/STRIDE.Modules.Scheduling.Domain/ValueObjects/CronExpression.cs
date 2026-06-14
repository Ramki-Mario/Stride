using STRIDE.Modules.Scheduling.Domain.Exceptions;

namespace STRIDE.Modules.Scheduling.Domain.ValueObjects;

/// <summary>
/// Validated 5-field cron expression (minute hour day-of-month month day-of-week).
/// </summary>
public sealed class CronExpression
{
    public string Value { get; }

    private CronExpression(string value) => Value = value;

    public static CronExpression Create(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new SchedulingDomainException("Cron expression cannot be empty.");

        var parts = expression.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 5)
            throw new SchedulingDomainException(
                "Cron expression must have exactly 5 fields: minute hour day-of-month month day-of-week.");

        return new CronExpression(expression.Trim());
    }

    public override string ToString() => Value;
    public override bool Equals(object? obj) => obj is CronExpression other && Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
}
