using System.Text;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.Commands.UploadInstanceAttachment;

namespace STRIDE.Modules.Workflows.Application.Tests;

public sealed class UploadInstanceAttachmentCommandHandlerTests
{
    private readonly IFileStorageService   _storage = Substitute.For<IFileStorageService>();
    private readonly IAttachmentRepository _repo    = Substitute.For<IAttachmentRepository>();
    private readonly UploadInstanceAttachmentCommandHandler _sut;

    public UploadInstanceAttachmentCommandHandlerTests()
    {
        _sut = new UploadInstanceAttachmentCommandHandler(_storage, _repo);

        _storage
            .UploadAsync(Arg.Any<Guid>(), Arg.Any<Stream>(), Arg.Any<string>(),
                         Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("tenant/2026/06/key-file.pdf");
    }

    // ── Validation ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenContentTypeNotAllowed_ReturnsFailure()
    {
        var result = await _sut.Handle(MakeCommand(contentType: "application/zip"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not permitted");
    }

    [Fact]
    public async Task Handle_WhenFileSizeExceedsLimit_ReturnsFailure()
    {
        var result = await _sut.Handle(
            MakeCommand(fileSizeBytes: 10L * 1024 * 1024 + 1), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("exceeds");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenFileNameBlank_ReturnsFailure(string fileName)
    {
        var result = await _sut.Handle(MakeCommand(fileName: fileName), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("name");
    }

    // ── Success ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsNewId()
    {
        var result = await _sut.Handle(MakeCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_WithValidCommand_AttachmentHasNullStepInstanceId()
    {
        Domain.Entities.Attachment? persisted = null;
        await _repo.AddAsync(
            Arg.Do<Domain.Entities.Attachment>(a => persisted = a),
            Arg.Any<CancellationToken>());

        await _sut.Handle(MakeCommand(), CancellationToken.None);

        persisted.Should().NotBeNull();
        persisted!.StepInstanceId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithValidCommand_PersistsAndSaves()
    {
        await _sut.Handle(MakeCommand(), CancellationToken.None);

        await _repo.Received(1).AddAsync(Arg.Any<Domain.Entities.Attachment>(), Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static UploadInstanceAttachmentCommand MakeCommand(
        string contentType   = "application/pdf",
        long   fileSizeBytes = 512,
        string fileName      = "contract.pdf")
    {
        return new UploadInstanceAttachmentCommand(
            TenantId:           Guid.NewGuid(),
            WorkflowInstanceId: Guid.NewGuid(),
            Content:            new MemoryStream(Encoding.UTF8.GetBytes("dummy")),
            FileName:           fileName,
            ContentType:        contentType,
            FileSizeBytes:      fileSizeBytes,
            UploadedBy:         Guid.NewGuid());
    }
}
