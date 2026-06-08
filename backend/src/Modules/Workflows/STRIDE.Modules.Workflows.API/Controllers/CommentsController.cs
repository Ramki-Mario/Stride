using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Workflows.API.Dtos;
using STRIDE.Modules.Workflows.Application.Commands.CreateComment;
using STRIDE.Modules.Workflows.Application.Commands.DeleteComment;
using STRIDE.Modules.Workflows.Application.Commands.EditComment;
using STRIDE.Modules.Workflows.Application.Queries.ListWorkflowComments;

namespace STRIDE.Modules.Workflows.API.Controllers;

/// <summary>
/// Comment threads scoped to a workflow instance.
///
///   GET    /api/workflows/instances/{instanceId}/comments               — list (paginated)
///   POST   /api/workflows/instances/{instanceId}/comments               — create
///   PUT    /api/workflows/instances/{instanceId}/comments/{commentId}   — edit (author only)
///   DELETE /api/workflows/instances/{instanceId}/comments/{commentId}   — delete (author or manager)
/// </summary>
[ApiController]
[Authorize]
[Route("api/workflows/instances/{instanceId:guid}/comments")]
public sealed class CommentsController : ControllerBase
{
    private const string NotFoundFragment = "not found";

    private readonly IMediator     _mediator;
    private readonly ICurrentUser  _currentUser;
    private readonly ITenantContext _tenantContext;

    public CommentsController(
        IMediator mediator,
        ICurrentUser currentUser,
        ITenantContext tenantContext)
    {
        _mediator      = mediator;
        _currentUser   = currentUser;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Returns a page of comments for the given instance, ordered oldest-first.
    /// Soft-deleted comments are included (body shows "(deleted)") to preserve thread structure.
    /// GET /api/workflows/instances/{instanceId}/comments?page=1&amp;pageSize=20
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedCommentsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListComments(
        Guid instanceId,
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new ListWorkflowCommentsQuery(instanceId, page, pageSize),
            cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : NotFound();
    }

    /// <summary>
    /// Posts a new comment on the workflow instance.
    /// POST /api/workflows/instances/{instanceId}/comments
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateComment(
        Guid instanceId,
        [FromBody] CreateCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new CreateCommentCommand(
                TenantId:           _tenantContext.TenantId,
                WorkflowInstanceId: instanceId,
                AuthorId:           _currentUser.UserId,
                Body:               request.Body),
            cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error!.Contains(NotFoundFragment, StringComparison.OrdinalIgnoreCase))
                return NotFound(new { detail = result.Error });
            return BadRequest(new { detail = result.Error });
        }

        return CreatedAtAction(
            nameof(ListComments),
            new { instanceId },
            new { commentId = result.Value });
    }

    /// <summary>
    /// Edits the body of an existing comment. Only the original author may edit.
    /// PUT /api/workflows/instances/{instanceId}/comments/{commentId}
    /// </summary>
    [HttpPut("{commentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EditComment(
        Guid instanceId,
        Guid commentId,
        [FromBody] EditCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new EditCommentCommand(
                CommentId: commentId,
                EditorId:  _currentUser.UserId,
                NewBody:   request.NewBody),
            cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error!.Contains(NotFoundFragment, StringComparison.OrdinalIgnoreCase))
                return NotFound(new { detail = result.Error });
            return BadRequest(new { detail = result.Error });
        }

        return NoContent();
    }

    /// <summary>
    /// Soft-deletes a comment (body replaced with "(deleted)").
    /// The original author or any user with the TenantAdmin role may delete.
    /// DELETE /api/workflows/instances/{instanceId}/comments/{commentId}
    /// </summary>
    [HttpDelete("{commentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteComment(
        Guid instanceId,
        Guid commentId,
        CancellationToken cancellationToken = default)
    {
        var isManager = _currentUser.Roles
            .Contains("TenantAdmin", StringComparer.OrdinalIgnoreCase);

        var result = await _mediator.Send(
            new DeleteCommentCommand(
                DeleterId: _currentUser.UserId,
                CommentId: commentId,
                IsManager: isManager),
            cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error!.Contains(NotFoundFragment, StringComparison.OrdinalIgnoreCase))
                return NotFound(new { detail = result.Error });
            return BadRequest(new { detail = result.Error });
        }

        return NoContent();
    }
}
