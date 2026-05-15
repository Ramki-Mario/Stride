using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Commands.ResumeWorkflow;

/// <summary>Resumes a Paused workflow instance.</summary>
public sealed record ResumeWorkflowCommand(
    Guid WorkflowInstanceId,
    Guid ResumedBy) : IRequest<Result>;
