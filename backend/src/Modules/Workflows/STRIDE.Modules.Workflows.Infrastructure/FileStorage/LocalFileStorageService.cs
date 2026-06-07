using Microsoft.Extensions.Configuration;
using STRIDE.Modules.Workflows.Application.Abstractions;

namespace STRIDE.Modules.Workflows.Infrastructure.FileStorage;

/// <summary>
/// Stores attachments on the local filesystem.
/// <para>
/// Intended for development only — no cloud credentials required.
/// Activated when <c>Storage:Provider = "local"</c> in configuration.
/// </para>
/// Storage layout: <c>{BasePath}/{tenantId}/{year}/{MM}/{uniqueId}-{sanitizedFileName}</c>
/// </summary>
internal sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public LocalFileStorageService(IConfiguration configuration)
    {
        _basePath = configuration["Storage:Local:BasePath"]
            ?? Path.Combine(Path.GetTempPath(), "stride-uploads");
    }

    /// <inheritdoc/>
    public async Task<string> UploadAsync(
        Guid tenantId,
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var uniqueName = $"{Guid.NewGuid():N}-{SanitizeFileName(fileName)}";

        // Build a tenant-scoped, date-partitioned storage key.
        // Forward slashes are used as separators regardless of OS so keys
        // remain consistent if the database is migrated to Azure later.
        var storageKey = $"{tenantId}/{now.Year}/{now.Month:D2}/{uniqueName}";
        var fullPath   = ToFullPath(storageKey);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var file = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: 81_920, useAsync: true);
        await content.CopyToAsync(file, cancellationToken);

        return storageKey;
    }

    /// <inheritdoc/>
    public Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ToFullPath(storageKey);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException(
                $"No local file found for storage key '{storageKey}'. " +
                "The file may have been deleted or the BasePath may have changed.", fullPath);

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 81_920, useAsync: true);
        return Task.FromResult(stream);
    }

    /// <inheritdoc/>
    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var fullPath = ToFullPath(storageKey);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    /// <exception cref="NotSupportedException">
    /// Always thrown — local storage has no signed-URL capability.
    /// Use <see cref="DownloadAsync"/> to stream the file through the API instead.
    /// </exception>
    public Task<string> GetDownloadUrlAsync(
        string storageKey,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException(
            "LocalFileStorageService does not support signed download URLs. " +
            "Stream the file through the API using DownloadAsync instead.");

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string ToFullPath(string storageKey) =>
        Path.Combine(_basePath, storageKey.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>
    /// Strips characters that are unsafe in filenames while preserving the extension.
    /// Collapses runs of invalid characters to a single underscore.
    /// </summary>
    private static string SanitizeFileName(string fileName)
    {
        var ext  = Path.GetExtension(fileName);
        var name = Path.GetFileNameWithoutExtension(fileName);
        var safe = string.Concat(name.Select(c =>
            char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_'));
        return safe + ext;
    }
}
