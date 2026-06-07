using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.Queries.ListStepAttachments;
using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Application.Tests;

public sealed class ListStepAttachmentsQueryHandlerTests
{
    private readonly IAttachmentRepository _repo = Substitute.For<IAttachmentRepository>();
    private readonly ListStepAttachmentsQueryHandler _sut;

    public ListStepAttachmentsQueryHandlerTests()
    {
        _sut = new ListStepAttachmentsQueryHandler(_repo);
    }

    // ── Empty result ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenNoAttachments_ReturnsSuccessWithEmptyList()
    {
        var stepId = Guid.NewGuid();
        _repo.GetByStepInstanceIdAsync(stepId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Attachment>());

        var result = await _sut.Handle(new ListStepAttachmentsQuery(stepId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    // ── DTO mapping ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithAttachments_MapsFieldsCorrectly()
    {
        var stepId     = Guid.NewGuid();
        var uploadedBy = Guid.NewGuid();
        var attachment = Attachment.Create(
            tenantId:           Guid.NewGuid(),
            workflowInstanceId: Guid.NewGuid(),
            stepInstanceId:     stepId,
            fileName:           "photo.jpg",
            contentType:        "image/jpeg",
            storageKey:         "tenant/2026/06/abc-photo.jpg",
            fileSizeBytes:      2048,
            uploadedByUserId:   uploadedBy);

        _repo.GetByStepInstanceIdAsync(stepId, Arg.Any<CancellationToken>())
            .Returns(new[] { attachment });

        var result = await _sut.Handle(new ListStepAttachmentsQuery(stepId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);

        var dto = result.Value[0];
        dto.Id.Should().Be(attachment.Id);
        dto.FileName.Should().Be("photo.jpg");
        dto.ContentType.Should().Be("image/jpeg");
        dto.FileSizeBytes.Should().Be(2048);
        dto.UploadedByUserId.Should().Be(uploadedBy);
    }

    // ── IsImage flag ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("image/jpeg",    true)]
    [InlineData("image/png",     true)]
    [InlineData("image/webp",    true)]
    [InlineData("application/pdf",  false)]
    [InlineData("text/plain",       false)]
    public async Task Handle_SetsIsImageBasedOnContentType(string contentType, bool expectedIsImage)
    {
        var stepId = Guid.NewGuid();
        var attachment = Attachment.Create(
            Guid.NewGuid(), Guid.NewGuid(), stepId,
            fileName: "file.dat", contentType: contentType,
            storageKey: "key", fileSizeBytes: 100,
            uploadedByUserId: Guid.NewGuid());

        _repo.GetByStepInstanceIdAsync(stepId, Arg.Any<CancellationToken>())
            .Returns(new[] { attachment });

        var result = await _sut.Handle(new ListStepAttachmentsQuery(stepId), CancellationToken.None);

        result.Value[0].IsImage.Should().Be(expectedIsImage);
    }

    // ── Multiple attachments ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithMultipleAttachments_ReturnsAllMapped()
    {
        var stepId   = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var wfId     = Guid.NewGuid();

        var attachments = Enumerable.Range(1, 3)
            .Select(i => Attachment.Create(
                tenantId, wfId, stepId,
                $"file{i}.pdf", "application/pdf",
                $"key{i}", fileSizeBytes: 100 * i,
                uploadedByUserId: Guid.NewGuid()))
            .ToArray();

        _repo.GetByStepInstanceIdAsync(stepId, Arg.Any<CancellationToken>())
            .Returns(attachments);

        var result = await _sut.Handle(new ListStepAttachmentsQuery(stepId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
    }
}
