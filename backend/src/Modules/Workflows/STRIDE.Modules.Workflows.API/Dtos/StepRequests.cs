namespace STRIDE.Modules.Workflows.API.Dtos;

/// <summary>Body for POST .../steps/{stepId}/assign</summary>
public sealed record AssignStepRequest
{
    public required Guid AssigneeId { get; init; }
}

/// <summary>Body for POST .../steps/{stepId}/fail</summary>
public sealed record FailStepRequest(string Reason);
