using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Commands.EditComment;

public sealed record EditCommentCommand(
    Guid   CommentId,
    Guid   EditorId,
    string NewBody) : IRequest<Result>;
