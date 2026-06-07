using System.Text;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.Commands.UploadStepAttachment;

namespace STRIDE.Modules.Workflows.Application.Tests;

public sealed class UploadStepAttachmentCommandHandlerTests
{
    private readonly IFileStorageService   _storage = Substitute.For<IFileStorageService>();
    private readonly IAttachmentRepository _repo    = Substitute.For<IAttachmentRepository>();
    private readonly UploadStepAttachmentCommandHandler _sut;

    public UploadStepAttachmentCommandHandlerTests()
    {
        _sut = new UploadStepAttachmentCommandHandler(_storage, _repo);

        // Default: storage returns a stable key
        _storage
            .UploadAsync(Arg.Any<Guid>(), Arg.Any<Stream>(), Arg.Any<string>(),
                         Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("tenant/2026/06/some-key-file.pdf");
    }

    // ── Validation — MIME type ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenContentTypeNotAllowed_ReturnsFailure()
    {
        var cmd = MakeCommand(contentType: "application/zip");

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not permitted");
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    [InlineData("application/pdf")]
    [InlineData("application/msword")]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("text/plain")]
    public async Task Handle_WhenContentTypeAllowed_DoesNotRejectOnMimeType(string mimeType)
    {
        var cmd = MakeCommand(contentType: mimeType);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        // May fail for other reasons (repo mock), but NOT a MIME-rejection
        result.Error.Should().NotContain("not permitted");
    }

    // ── Validation — file size ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenFileSizeExceedsLimit_ReturnsFailure()
    {
        var cmd = MakeCommand(fileSizeBytes: 10L * 1024 * 1024 + 1);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("exceeds");
    }

    [Fact]
    public async Task Handle_WhenFileSizeAtLimit_PassesSizeValidation()
    {
        var cmd = MakeCommand(fileSizeBytes: 10L * 1024 * 1024);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Error.Should().NotContain("exceeds");
    }

    // ── Validation — file name ────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenFileNameBlank_ReturnsFailure(string fileName)
    {
        var cmd = MakeCommand(fileName: fileName);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("name");
    }

    // ── Success path ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsSuccessWithNewId()
    {
        var cmd = MakeCommand();

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_WithValidCommand_UploadsToStorage()
    {
        var cmd = MakeCommand();

        await _sut.Handle(cmd, CancellationToken.None);

        await _storage.Received(1).UploadAsync(
            cmd.TenantId,
            cmd.Content,
            cmd.FileName,
            cmd.ContentType,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidCommand_PersistsAttachment()
    {
        var cmd = MakeCommand();

        await _sut.Handle(cmd, CancellationToken.None);

        await _repo.Received(1).AddAsync(Arg.Any<Domain.Entities.Attachment>(), Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static UploadStepAttachmentCommand MakeCommand(
        string contentType  = "application/pdf",
        long   fileSizeBytes = 512,
        string fileName      = "report.pdf")
    {
        return new UploadStepAttachmentCommand(
            TenantId:           Guid.NewGuid(),
            WorkflowInstanceId: Guid.NewGuid(),
            StepInstanceId:     Guid.NewGuid(),
            Content:            new MemoryStream(Encoding.UTF8.GetBytes("dummy")),
            FileName:           fileName,
            ContentType:        contentType,
            FileSizeBytes:      fileSizeBytes,
            UploadedBy:         Guid.NewGuid());
    }
}
