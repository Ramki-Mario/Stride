namespace STRIDE.Modules.Workflows.Application.DTOs;

/// <summary>
/// Read-facing DTO returned when listing attachments on a step instance.
/// </summary>
public sealed record AttachmentDto(
    Guid     Id,
    string   FileName,
    string   ContentType,
    long     FileSizeBytes,
    Guid     UploadedByUserId,
    DateTime CreatedAt,
    bool     IsImage);

/// <summary>
/// Extended DTO returned when listing all attachments on a workflow instance
/// (both step-level and instance-level).
/// <para>
/// <c>StepInstanceId</c> and <c>StepName</c> are null for instance-level attachments
/// (documents that belong to the job as a whole, not a specific step).
/// </para>
/// </summary>
public sealed record WorkflowAttachmentDto(
    Guid     Id,
    string   FileName,
    string   ContentType,
    long     FileSizeBytes,
    Guid     UploadedByUserId,
    DateTime CreatedAt,
    bool     IsImage,
    Guid?    StepInstanceId,
    string?  StepName);
