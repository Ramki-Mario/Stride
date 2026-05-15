using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Commands.SkipStep;

/// <summary>
/// Skips an optional (non-required) step in a Running workflow.
/// Required steps cannot be skipped — use <see cref="FailStepCommand"/> instead.
/// </summary>
public sealed record SkipStepCommand(
    Guid WorkflowInstanceId,
    Guid StepInstanceId,
    Guid SkippedBy) : IRequest<Result>;
