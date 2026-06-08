using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Commands.DeleteComment;

public sealed record DeleteCommentCommand(
    Guid DeleterId,
    Guid CommentId,
    bool IsManager) : IRequest<Result>;
