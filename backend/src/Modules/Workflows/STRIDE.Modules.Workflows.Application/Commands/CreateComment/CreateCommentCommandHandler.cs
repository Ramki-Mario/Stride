using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Application.Commands.CreateComment;

internal sealed class CreateCommentCommandHandler
    : IRequestHandler<CreateCommentCommand, Result<Guid>>
{
    private readonly IWorkflowCommentRepository  _comments;
    private readonly IWorkflowInstanceRepository _instances;

    public CreateCommentCommandHandler(
        IWorkflowCommentRepository  comments,
        IWorkflowInstanceRepository instances)
    {
        _comments  = comments;
        _instances = instances;
    }

    public async Task<Result<Guid>> Handle(
        CreateCommentCommand request,
        CancellationToken cancellationToken)
    {
        // Verify the instance exists and belongs to this tenant before allowing a comment.
        var instance = await _instances.GetByIdAsync(request.WorkflowInstanceId, cancellationToken);
        if (instance is null)
            return Result.Failure<Guid>(
                $"Workflow instance '{request.WorkflowInstanceId}' not found.");

        WorkflowComment comment;
        try
        {
            comment = WorkflowComment.Create(
                request.WorkflowInstanceId,
                request.TenantId,
                request.AuthorId,
                request.Body,
                instance.WorkflowName);
        }
        catch (WorkflowDomainException ex)
        {
            return Result.Failure<Guid>(ex.Message);
        }

        await _comments.AddAsync(comment, cancellationToken);
        await _comments.SaveChangesAsync(cancellationToken);

        return Result.Success(comment.Id);
    }
}
