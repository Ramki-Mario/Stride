using STRIDE.BuildingBlocks.Domain.Entities;

namespace STRIDE.Modules.Workflows.Domain.Entities;

/// <summary>
/// A file attached to a workflow instance or one of its step instances.
/// <para>
/// <c>StepInstanceId</c> is null when the file belongs to the overall job
/// (e.g. a signed contract or purchase order); non-null when it documents
/// the completion of a specific step (e.g. a photo of the finished repair).
/// </para>
/// Attachments are soft-deleted so the audit trail is never lost.
/// </summary>
public sealed class Attachment : AuditableEntity
{
    public Guid WorkflowInstanceId { get; private set; }

    /// <summary>Null for instance-level attachments; set for step-level attachments.</summary>
    public Guid? StepInstanceId { get; private set; }

    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;

    /// <summary>
    /// Opaque key returned by <see cref="Application.Abstractions.IFileStorageService.UploadAsync"/>.
    /// Stored and later passed back to the storage service for download/delete operations.
    /// Never exposed directly to clients.
    /// </summary>
    public string StorageKey { get; private set; } = string.Empty;

    public long FileSizeBytes { get; private set; }

    // AuditableEntity.CreatedBy is the uploading user.

    private Attachment() { }

    public static Attachment Create(
        Guid tenantId,
        Guid workflowInstanceId,
        Guid? stepInstanceId,
        string fileName,
        string contentType,
        string storageKey,
        long fileSizeBytes,
        Guid uploadedByUserId)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty.", nameof(fileName));
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Content type cannot be empty.", nameof(contentType));
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("Storage key cannot be empty.", nameof(storageKey));
        if (fileSizeBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(fileSizeBytes), "File size must be positive.");

        return new Attachment
        {
            Id                  = Guid.NewGuid(),
            TenantId            = tenantId,
            WorkflowInstanceId  = workflowInstanceId,
            StepInstanceId      = stepInstanceId,
            FileName            = fileName.Trim(),
            ContentType         = contentType.Trim(),
            StorageKey          = storageKey,
            FileSizeBytes       = fileSizeBytes,
            CreatedBy           = uploadedByUserId,
            CreatedAt           = DateTime.UtcNow,
            UpdatedAt           = DateTime.UtcNow,
        };
    }

    /// <summary>Soft-deletes the attachment. The storage key is retained for deferred cleanup.</summary>
    public void SoftDelete()
    {
        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
