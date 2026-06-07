namespace STRIDE.Modules.Workflows.Application.Abstractions;

/// <summary>
/// Abstraction over file storage backends.
/// <para>
/// Two implementations exist:
/// <list type="bullet">
///   <item><see cref="string">LocalFileStorageService</see> — writes to the local filesystem; for development only.</item>
///   <item><see cref="string">AzureBlobStorageService</see> — writes to Azure Blob Storage; for production.</item>
/// </list>
/// Activated via the <c>Storage:Provider</c> configuration key (<c>local</c> | <c>azure</c>).
/// </para>
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Uploads a file and returns a storage key — an opaque string that uniquely
    /// identifies the stored file and is used by all subsequent operations.
    /// The key is scoped to the tenant (prefixed with <paramref name="tenantId"/>)
    /// so files can never be cross-tenant accessed.
    /// </summary>
    Task<string> UploadAsync(
        Guid tenantId,
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the raw file content as a readable stream.</summary>
    Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently removes the stored file. Safe to call on a key that no longer exists.
    /// Callers should soft-delete the <c>Attachment</c> entity separately.
    /// </summary>
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a time-limited direct-download URL (e.g. an Azure SAS URL).
    /// <para>
    /// Not supported by all providers: <c>LocalFileStorageService</c> throws
    /// <see cref="NotSupportedException"/> — use <see cref="DownloadAsync"/> instead.
    /// </para>
    /// </summary>
    Task<string> GetDownloadUrlAsync(
        string storageKey,
        TimeSpan expiry,
        CancellationToken cancellationToken = default);
}
