using Microsoft.Extensions.Configuration;
using STRIDE.Modules.Workflows.Infrastructure.FileStorage;

namespace STRIDE.Modules.Workflows.Application.Tests;

/// <summary>
/// Unit tests for <see cref="LocalFileStorageService"/>.
/// Each test uses its own isolated temp directory so tests never interfere with each other.
/// </summary>
public sealed class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly LocalFileStorageService _sut;

    public LocalFileStorageServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"stride-tests-{Guid.NewGuid():N}");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:Local:BasePath"] = _tempRoot,
            })
            .Build();

        _sut = new LocalFileStorageService(config);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, recursive: true);
    }

    // ── Upload ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UploadAsync_WritesFileToDisk_AndReturnsNonEmptyStorageKey()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var content  = CreateStream("hello world");

        // Act
        var storageKey = await _sut.UploadAsync(tenantId, content, "test.txt", "text/plain");

        // Assert
        storageKey.Should().NotBeNullOrWhiteSpace();
        var fullPath = Path.Combine(_tempRoot, storageKey.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(fullPath).Should().BeTrue("the file should be written to disk");
    }

    [Fact]
    public async Task UploadAsync_StorageKeyIsScopedToTenant()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var content  = CreateStream("data");

        // Act
        var storageKey = await _sut.UploadAsync(tenantId, content, "file.pdf", "application/pdf");

        // Assert — key starts with the tenant ID
        storageKey.Should().StartWith(tenantId.ToString(),
            "storage keys must be prefixed with tenantId to prevent cross-tenant access");
    }

    [Fact]
    public async Task UploadAsync_TwoCallsWithSameFileName_ProduceDistinctKeys()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var key1 = await _sut.UploadAsync(tenantId, CreateStream("a"), "photo.jpg", "image/jpeg");
        var key2 = await _sut.UploadAsync(tenantId, CreateStream("b"), "photo.jpg", "image/jpeg");

        // Assert — each upload gets a unique key even for the same filename
        key1.Should().NotBe(key2);
    }

    [Fact]
    public async Task UploadAsync_FileNameWithSpecialChars_IsSanitized()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var storageKey = await _sut.UploadAsync(
            tenantId, CreateStream("x"), "my dangerous/../file name!.txt", "text/plain");

        // Assert — the resulting file actually exists (sanitised name didn't break the path)
        var fullPath = Path.Combine(_tempRoot, storageKey.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(fullPath).Should().BeTrue();
    }

    // ── Download ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task DownloadAsync_ReturnsCorrectContent()
    {
        // Arrange
        var expectedText = "attachment content 123";
        var tenantId     = Guid.NewGuid();
        var storageKey   = await _sut.UploadAsync(tenantId, CreateStream(expectedText), "doc.txt", "text/plain");

        // Act
        await using var stream = await _sut.DownloadAsync(storageKey);
        using var reader = new StreamReader(stream);
        var actual = await reader.ReadToEndAsync();

        // Assert
        actual.Should().Be(expectedText);
    }

    [Fact]
    public async Task DownloadAsync_WhenKeyDoesNotExist_ThrowsFileNotFoundException()
    {
        // Act
        var act = async () => await _sut.DownloadAsync("nonexistent/key/file.txt");

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_RemovesFileFromDisk()
    {
        // Arrange
        var tenantId   = Guid.NewGuid();
        var storageKey = await _sut.UploadAsync(tenantId, CreateStream("to be deleted"), "bye.txt", "text/plain");
        var fullPath   = Path.Combine(_tempRoot, storageKey.Replace('/', Path.DirectorySeparatorChar));

        // Pre-condition
        File.Exists(fullPath).Should().BeTrue();

        // Act
        await _sut.DeleteAsync(storageKey);

        // Assert
        File.Exists(fullPath).Should().BeFalse("file should be gone after deletion");
    }

    [Fact]
    public async Task DeleteAsync_WhenKeyDoesNotExist_DoesNotThrow()
    {
        // Act
        var act = async () => await _sut.DeleteAsync("ghost/key/missing.txt");

        // Assert — should be a no-op
        await act.Should().NotThrowAsync();
    }

    // ── GetDownloadUrlAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetDownloadUrlAsync_ThrowsNotSupportedException()
    {
        // Act
        var act = async () => await _sut.GetDownloadUrlAsync("any/key.txt", TimeSpan.FromHours(1));

        // Assert — local storage has no signed-URL capability
        await act.Should().ThrowAsync<NotSupportedException>();
    }

    // ── Default base path ─────────────────────────────────────────────────────

    [Fact]
    public async Task UploadAsync_WhenBasePathNotConfigured_UsesSystemTempFolder()
    {
        // Arrange — build service without BasePath config
        var config = new ConfigurationBuilder().Build();
        var sut    = new LocalFileStorageService(config);
        var key    = await sut.UploadAsync(Guid.NewGuid(), CreateStream("x"), "tmp.txt", "text/plain");

        try
        {
            // Assert — file should be under the system temp folder
            var expectedRoot = Path.Combine(Path.GetTempPath(), "stride-uploads");
            var fullPath     = Path.Combine(expectedRoot, key.Replace('/', Path.DirectorySeparatorChar));
            File.Exists(fullPath).Should().BeTrue();
        }
        finally
        {
            // Cleanup — don't leave stray files in the real temp directory
            await sut.DeleteAsync(key);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Stream CreateStream(string content)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        return new MemoryStream(bytes);
    }
}
