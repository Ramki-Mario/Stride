namespace STRIDE.Modules.Teams.Domain.Exceptions;

public sealed class TeamDomainException : Exception
{
    public TeamDomainException(string message) : base(message) { }
}
