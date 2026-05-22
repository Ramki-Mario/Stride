namespace STRIDE.Modules.Invoicing.Domain.Exceptions;

public sealed class InvoiceDomainException : Exception
{
    public InvoiceDomainException(string message) : base(message) { }
}
