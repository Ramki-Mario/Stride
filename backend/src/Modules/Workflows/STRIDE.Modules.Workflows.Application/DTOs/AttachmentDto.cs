namespace STRIDE.Modules.Workflows.Application.DTOs;

/// <summary>
/// Read-facing DTO returned when listing attachments on a step or workflow instance.
/// </summary>
public sealed record AttachmentDto(
    Guid     Id,
    string   FileName,
    string   ContentType,
    long     FileSizeBytes,
    Guid     UploadedByUserId,
    DateTime CreatedAt,
    bool     IsImage);
