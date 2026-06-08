using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Application.Commands.EditComment;

internal sealed class EditCommentCommandHandler
    : IRequestHandler<EditCommentCommand, Result>
{
    private readonly IWorkflowCommentRepository _comments;

    public EditCommentCommandHandler(IWorkflowCommentRepository comments)
        => _comments = comments;

    public async Task<Result> Handle(
        EditCommentCommand request,
        CancellationToken cancellationToken)
    {
        var comment = await _comments.GetByIdAsync(request.CommentId, cancellationToken);
        if (comment is null)
            return Result.Failure($"Comment '{request.CommentId}' not found.");

        try
        {
            comment.Edit(request.NewBody, request.EditorId);
        }
        catch (WorkflowDomainException ex)
        {
            return Result.Failure(ex.Message);
        }

        await _comments.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
