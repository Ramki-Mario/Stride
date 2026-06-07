using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.Commands.DeleteAttachment;
using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Application.Tests;

public sealed class DeleteAttachmentCommandHandlerTests
{
    private readonly IAttachmentRepository _repo    = Substitute.For<IAttachmentRepository>();
    private readonly IFileStorageService   _storage = Substitute.For<IFileStorageService>();
    private readonly DeleteAttachmentCommandHandler _sut;

    public DeleteAttachmentCommandHandlerTests()
    {
        _sut = new DeleteAttachmentCommandHandler(_repo, _storage);
    }

    // ── Not found ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenAttachmentNotFound_ReturnsFailure()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Attachment?)null);

        var cmd = new DeleteAttachmentCommand(Guid.NewGuid(), Guid.NewGuid(), false);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    // ── Permission checks ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenCallerIsNeitherUploaderNorAdmin_ReturnsFailure()
    {
        var uploader   = Guid.NewGuid();
        var attachment = BuildAttachment(uploader);
        _repo.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>())
            .Returns(attachment);

        // Different user, not manager
        var cmd = new DeleteAttachmentCommand(attachment.Id, Guid.NewGuid(), IsManagerOrAdmin: false);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("uploader or a manager");
    }

    [Fact]
    public async Task Handle_WhenCallerIsUploader_ReturnsSuccess()
    {
        var uploader   = Guid.NewGuid();
        var attachment = BuildAttachment(uploader);
        _repo.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>())
            .Returns(attachment);

        var cmd = new DeleteAttachmentCommand(attachment.Id, uploader, IsManagerOrAdmin: false);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenCallerIsManagerNotUploader_ReturnsSuccess()
    {
        var uploader   = Guid.NewGuid();
        var attachment = BuildAttachment(uploader);
        _repo.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>())
            .Returns(attachment);

        var cmd = new DeleteAttachmentCommand(attachment.Id, Guid.NewGuid(), IsManagerOrAdmin: true);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    // ── Persistence ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_OnSuccess_SoftDeletesAndSaves()
    {
        var uploader   = Guid.NewGuid();
        var attachment = BuildAttachment(uploader);
        _repo.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>())
            .Returns(attachment);

        var cmd = new DeleteAttachmentCommand(attachment.Id, uploader, IsManagerOrAdmin: false);

        await _sut.Handle(cmd, CancellationToken.None);

        attachment.IsDeleted.Should().BeTrue();
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Physical file removal ─────────────────────────────────────────────────

    [Fact]
    public async Task Handle_OnSuccess_DeletesPhysicalFile()
    {
        var uploader   = Guid.NewGuid();
        var attachment = BuildAttachment(uploader);
        _repo.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>())
            .Returns(attachment);

        var cmd = new DeleteAttachmentCommand(attachment.Id, uploader, IsManagerOrAdmin: false);

        await _sut.Handle(cmd, CancellationToken.None);

        await _storage.Received(1).DeleteAsync(attachment.StorageKey, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenStorageDeleteThrows_StillReturnsSuccess()
    {
        var uploader   = Guid.NewGuid();
        var attachment = BuildAttachment(uploader);
        _repo.GetByIdAsync(attachment.Id, Arg.Any<CancellationToken>())
            .Returns(attachment);
        _storage
            .DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new IOException("Storage unreachable")));

        var cmd = new DeleteAttachmentCommand(attachment.Id, uploader, IsManagerOrAdmin: false);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        // Best-effort: physical failure must not bubble up
        result.IsSuccess.Should().BeTrue();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Attachment BuildAttachment(Guid uploadedBy)
    {
        return Attachment.Create(
            tenantId:           Guid.NewGuid(),
            workflowInstanceId: Guid.NewGuid(),
            stepInstanceId:     Guid.NewGuid(),
            fileName:           "document.pdf",
            contentType:        "application/pdf",
            storageKey:         "tenant/2026/06/abc-document.pdf",
            fileSizeBytes:      1024,
            uploadedByUserId:   uploadedBy);
    }
}
