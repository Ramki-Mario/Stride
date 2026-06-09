using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Commands.ResumeFromHalt;

public sealed record ResumeFromHaltCommand(
    Guid WorkflowInstanceId,
    Guid ResumedBy) : IRequest<Result>;
