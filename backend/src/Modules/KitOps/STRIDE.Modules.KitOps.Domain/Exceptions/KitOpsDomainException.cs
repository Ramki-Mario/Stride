namespace STRIDE.Modules.KitOps.Domain.Exceptions;

public sealed class KitOpsDomainException : Exception
{
    public KitOpsDomainException(string message) : base(message) { }
}
