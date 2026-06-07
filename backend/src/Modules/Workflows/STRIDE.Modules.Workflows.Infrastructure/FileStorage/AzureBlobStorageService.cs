using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using STRIDE.Modules.Workflows.Application.Abstractions;

namespace STRIDE.Modules.Workflows.Infrastructure.FileStorage;

/// <summary>
/// Stores attachments in Azure Blob Storage.
/// <para>
/// Activated when <c>Storage:Provider = "azure"</c> in configuration.
/// Requires <c>Storage:Azure:ConnectionString</c> (via environment variable or Azure Key Vault —
/// never in committed config files).
/// Optional: <c>Storage:Azure:ContainerName</c> (defaults to <c>stride-attachments</c>).
/// </para>
/// Storage layout: <c>{tenantId}/{year}/{MM}/{uniqueId}-{fileName}</c> within the container.
/// Files are always stored with <c>Private</c> access — download URLs use time-limited SAS tokens.
/// </summary>
internal sealed class AzureBlobStorageService : IFileStorageService
{
    private readonly BlobServiceClient _serviceClient;
    private readonly string _containerName;

    public AzureBlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration["Storage:Azure:ConnectionString"]
            ?? throw new InvalidOperationException(
                "Storage:Azure:ConnectionString must be provided when Storage:Provider = 'azure'. " +
                "Set it via an environment variable or Azure Key Vault — never in appsettings.json.");

        _containerName = configuration["Storage:Azure:ContainerName"] ?? "stride-attachments";
        _serviceClient = new BlobServiceClient(connectionString);
    }

    /// <inheritdoc/>
    public async Task<string> UploadAsync(
        Guid tenantId,
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var now       = DateTime.UtcNow;
        var blobPath  = $"{tenantId}/{now.Year}/{now.Month:D2}/{Guid.NewGuid():N}-{fileName}";
        var container = await GetOrCreateContainerAsync(cancellationToken);
        var blob      = container.GetBlobClient(blobPath);

        await blob.UploadAsync(content,
            new BlobHttpHeaders { ContentType = contentType },
            cancellationToken: cancellationToken);

        return blobPath;
    }

    /// <inheritdoc/>
    public async Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var blob     = GetBlobClient(storageKey);
        var response = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return response.Value.Content;
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var blob = GetBlobClient(storageKey);
        await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Generates a SAS URI valid for <paramref name="expiry"/>.
    /// The blob must exist and the service client must be authorized with a
    /// storage account key or user-delegation key (not a SAS connection string).
    /// </remarks>
    public Task<string> GetDownloadUrlAsync(
        string storageKey,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        var blob   = GetBlobClient(storageKey);
        var sasUri = blob.GenerateSasUri(
            BlobSasPermissions.Read,
            DateTimeOffset.UtcNow.Add(expiry));
        return Task.FromResult(sasUri.ToString());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<BlobContainerClient> GetOrCreateContainerAsync(CancellationToken ct)
    {
        var container = _serviceClient.GetBlobContainerClient(_containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
        return container;
    }

    private BlobClient GetBlobClient(string storageKey) =>
        _serviceClient.GetBlobContainerClient(_containerName).GetBlobClient(storageKey);
}
