namespace STRIDE.Modules.Clients.Domain.Exceptions;

public sealed class ClientDomainException : Exception
{
    public ClientDomainException(string message) : base(message) { }
}
