using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.Commands.DeleteAttachment;
using STRIDE.Modules.Workflows.Application.Commands.UploadStepAttachment;
using STRIDE.Modules.Workflows.Application.DTOs;
using STRIDE.Modules.Workflows.Application.Queries.DownloadAttachment;
using STRIDE.Modules.Workflows.Application.Queries.ListStepAttachments;

namespace STRIDE.Modules.Workflows.API.Controllers;

/// <summary>
/// File attachments scoped to a step instance.
///
///   POST   /api/workflows/instances/{instanceId}/steps/{stepId}/attachments          — upload a file
///   GET    /api/workflows/instances/{instanceId}/steps/{stepId}/attachments          — list attachments
///   GET    /api/workflows/instances/{instanceId}/steps/{stepId}/attachments/{id}/download — stream file
///   DELETE /api/workflows/instances/{instanceId}/steps/{stepId}/attachments/{id}     — soft-delete
///
/// File type allowlist (server-enforced): image/jpeg, image/png, image/webp,
/// application/pdf, application/msword, text/plain.
/// Maximum file size: 10 MB.
/// </summary>
[ApiController]
[Authorize]
[Route("api/workflows/instances/{instanceId:guid}/steps/{stepId:guid}/attachments")]
public sealed class StepAttachmentsController : ControllerBase
{
    private readonly IMediator      _mediator;
    private readonly ICurrentUser   _currentUser;
    private readonly ITenantContext _tenantContext;

    public StepAttachmentsController(
        IMediator mediator,
        ICurrentUser currentUser,
        ITenantContext tenantContext)
    {
        _mediator      = mediator;
        _currentUser   = currentUser;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Upload a file and attach it to the step instance.
    /// POST /api/workflows/instances/{instanceId}/steps/{stepId}/attachments
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(
        Guid instanceId,
        Guid stepId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file provided or the file is empty." });

        await using var stream = file.OpenReadStream();

        var result = await _mediator.Send(
            new UploadStepAttachmentCommand(
                TenantId:           _tenantContext.TenantId,
                WorkflowInstanceId: instanceId,
                StepInstanceId:     stepId,
                Content:            stream,
                FileName:           file.FileName,
                ContentType:        file.ContentType,
                FileSizeBytes:      file.Length,
                UploadedBy:         _currentUser.UserId),
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return StatusCode(StatusCodes.Status201Created, new { id = result.Value });
    }

    /// <summary>
    /// List all attachments for this step instance.
    /// GET /api/workflows/instances/{instanceId}/steps/{stepId}/attachments
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AttachmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        Guid instanceId,
        Guid stepId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ListStepAttachmentsQuery(stepId),
            cancellationToken);

        return Ok(result.Value);
    }

    /// <summary>
    /// Stream a file back to the caller.
    /// GET /api/workflows/instances/{instanceId}/steps/{stepId}/attachments/{attachmentId}/download
    /// </summary>
    [HttpGet("{attachmentId:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(
        Guid instanceId,
        Guid stepId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new DownloadAttachmentQuery(attachmentId),
            cancellationToken);

        if (result.IsFailure)
            return NotFound(new { error = result.Error });

        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    /// <summary>
    /// Soft-delete an attachment. Only the uploader or a manager/admin may delete.
    /// DELETE /api/workflows/instances/{instanceId}/steps/{stepId}/attachments/{attachmentId}
    /// </summary>
    [HttpDelete("{attachmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid instanceId,
        Guid stepId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        var isManagerOrAdmin =
            _currentUser.Roles.Contains("admin",   StringComparer.OrdinalIgnoreCase) ||
            _currentUser.Roles.Contains("manager", StringComparer.OrdinalIgnoreCase);

        var result = await _mediator.Send(
            new DeleteAttachmentCommand(
                AttachmentId:     attachmentId,
                DeletedBy:        _currentUser.UserId,
                IsManagerOrAdmin: isManagerOrAdmin),
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error!.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return NotFound(new { error = result.Error });

            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }
}
