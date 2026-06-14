namespace STRIDE.Modules.Scheduling.Domain.Exceptions;

public sealed class SchedulingDomainException : Exception
{
    public SchedulingDomainException(string message) : base(message) { }
}
