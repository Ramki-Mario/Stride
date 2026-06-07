using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Workflows.Application.Queries.DownloadAttachment;

/// <summary>Returns the raw file stream and metadata for a single attachment.</summary>
public sealed record DownloadAttachmentQuery(
    Guid AttachmentId) : IRequest<Result<AttachmentDownloadResult>>;

/// <summary>File content + metadata needed to write the HTTP response.</summary>
public sealed record AttachmentDownloadResult(
    Stream Content,
    string FileName,
    string ContentType);
