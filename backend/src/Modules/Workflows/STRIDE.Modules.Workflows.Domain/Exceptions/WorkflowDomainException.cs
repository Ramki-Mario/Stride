namespace STRIDE.Modules.Workflows.Domain.Exceptions;

/// <summary>
/// Thrown when a domain rule is violated within the Workflow bounded context
/// (e.g. invalid state transitions, missing required data).
/// </summary>
public sealed class WorkflowDomainException : Exception
{
    public WorkflowDomainException(string message) : base(message) { }
}
