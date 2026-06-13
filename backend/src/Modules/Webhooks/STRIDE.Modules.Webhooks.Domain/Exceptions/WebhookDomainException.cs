namespace STRIDE.Modules.Webhooks.Domain.Exceptions;

/// <summary>Raised when a webhook aggregate invariant is violated.</summary>
public sealed class WebhookDomainException : Exception
{
    public WebhookDomainException(string message) : base(message) { }
}
