using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Commands.CreateComment;

public sealed record CreateCommentCommand(
    Guid   TenantId,
    Guid   WorkflowInstanceId,
    Guid   AuthorId,
    string Body) : IRequest<Result<Guid>>;
