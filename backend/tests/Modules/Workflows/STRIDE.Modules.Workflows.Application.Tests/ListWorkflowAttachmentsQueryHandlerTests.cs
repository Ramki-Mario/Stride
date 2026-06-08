using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.Queries.ListWorkflowAttachments;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;

namespace STRIDE.Modules.Workflows.Application.Tests;

public sealed class ListWorkflowAttachmentsQueryHandlerTests
{
    private readonly IAttachmentRepository       _attachmentRepo = Substitute.For<IAttachmentRepository>();
    private readonly IWorkflowInstanceRepository _instanceRepo   = Substitute.For<IWorkflowInstanceRepository>();
    private readonly ListWorkflowAttachmentsQueryHandler _sut;

    public ListWorkflowAttachmentsQueryHandlerTests()
    {
        _sut = new ListWorkflowAttachmentsQueryHandler(_attachmentRepo, _instanceRepo);
    }

    // ── Empty result ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenNoAttachments_ReturnsSuccessWithEmptyList()
    {
        var instanceId = Guid.NewGuid();
        _attachmentRepo.GetByWorkflowInstanceIdAsync(instanceId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Attachment>());
        _instanceRepo.GetByIdAsync(instanceId, Arg.Any<CancellationToken>())
            .Returns((WorkflowInstance?)null);

        var result = await _sut.Handle(new ListWorkflowAttachmentsQuery(instanceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    // ── Instance-level attachment (no step) ───────────────────────────────────

    [Fact]
    public async Task Handle_WithInstanceLevelAttachment_StepNameIsNull()
    {
        var instanceId = Guid.NewGuid();
        var att = Attachment.Create(
            Guid.NewGuid(), instanceId, null,
            "contract.pdf", "application/pdf",
            "key", 1024, Guid.NewGuid());

        _attachmentRepo.GetByWorkflowInstanceIdAsync(instanceId, Arg.Any<CancellationToken>())
            .Returns(new[] { att });
        _instanceRepo.GetByIdAsync(instanceId, Arg.Any<CancellationToken>())
            .Returns((WorkflowInstance?)null);

        var result = await _sut.Handle(new ListWorkflowAttachmentsQuery(instanceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].StepInstanceId.Should().BeNull();
        result.Value[0].StepName.Should().BeNull();
    }

    // ── Step-level attachment enriched with step name ─────────────────────────

    [Fact]
    public async Task Handle_WithStepLevelAttachment_EnrichesStepName()
    {
        var instanceId = Guid.NewGuid();
        var instance   = BuildInstance(instanceId);
        var stepId     = instance.Steps[0].Id;

        var att = Attachment.Create(
            instance.TenantId, instanceId, stepId,
            "photo.jpg", "image/jpeg",
            "key", 512, Guid.NewGuid());

        _attachmentRepo.GetByWorkflowInstanceIdAsync(instanceId, Arg.Any<CancellationToken>())
            .Returns(new[] { att });
        _instanceRepo.GetByIdAsync(instanceId, Arg.Any<CancellationToken>())
            .Returns(instance);

        var result = await _sut.Handle(new ListWorkflowAttachmentsQuery(instanceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var dto = result.Value[0];
        dto.StepInstanceId.Should().Be(stepId);
        dto.StepName.Should().NotBeNullOrEmpty();
    }

    // ── StepInstanceId points to unknown step ─────────────────────────────────

    [Fact]
    public async Task Handle_WhenStepNotFoundInInstance_StepNameIsNull()
    {
        var instanceId = Guid.NewGuid();
        // Attachment references a step that is NOT part of the instance
        var orphanStepId = Guid.NewGuid();
        var att = Attachment.Create(
            Guid.NewGuid(), instanceId, orphanStepId,
            "file.pdf", "application/pdf", "key", 256, Guid.NewGuid());

        _attachmentRepo.GetByWorkflowInstanceIdAsync(instanceId, Arg.Any<CancellationToken>())
            .Returns(new[] { att });
        _instanceRepo.GetByIdAsync(instanceId, Arg.Any<CancellationToken>())
            .Returns(BuildInstance(instanceId));

        var result = await _sut.Handle(new ListWorkflowAttachmentsQuery(instanceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value[0].StepName.Should().BeNull();
    }

    // ── Mixed: instance-level + step-level ────────────────────────────────────

    [Fact]
    public async Task Handle_WithMixedAttachments_ReturnsBoth()
    {
        var instanceId = Guid.NewGuid();
        var instance   = BuildInstance(instanceId);
        var stepId     = instance.Steps[0].Id;

        var instanceAtt = Attachment.Create(
            instance.TenantId, instanceId, null,
            "contract.pdf", "application/pdf", "key1", 1024, Guid.NewGuid());
        var stepAtt = Attachment.Create(
            instance.TenantId, instanceId, stepId,
            "photo.jpg", "image/jpeg", "key2", 512, Guid.NewGuid());

        _attachmentRepo.GetByWorkflowInstanceIdAsync(instanceId, Arg.Any<CancellationToken>())
            .Returns(new[] { instanceAtt, stepAtt });
        _instanceRepo.GetByIdAsync(instanceId, Arg.Any<CancellationToken>())
            .Returns(instance);

        var result = await _sut.Handle(new ListWorkflowAttachmentsQuery(instanceId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Count(d => d.StepInstanceId is null).Should().Be(1);
        result.Value.Count(d => d.StepInstanceId is not null).Should().Be(1);
    }

    // ── IsImage flag ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("image/jpeg",    true)]
    [InlineData("application/pdf", false)]
    public async Task Handle_SetsIsImageFlag(string contentType, bool expected)
    {
        var instanceId = Guid.NewGuid();
        var att = Attachment.Create(
            Guid.NewGuid(), instanceId, null,
            "file", contentType, "key", 100, Guid.NewGuid());

        _attachmentRepo.GetByWorkflowInstanceIdAsync(instanceId, Arg.Any<CancellationToken>())
            .Returns(new[] { att });
        _instanceRepo.GetByIdAsync(instanceId, Arg.Any<CancellationToken>())
            .Returns((WorkflowInstance?)null);

        var result = await _sut.Handle(new ListWorkflowAttachmentsQuery(instanceId), CancellationToken.None);

        result.Value[0].IsImage.Should().Be(expected);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    /// Builds a minimal started WorkflowInstance with 2 steps.
    private static WorkflowInstance BuildInstance(Guid instanceId)
    {
        var def = WorkflowDefinition.Create(Guid.NewGuid(), "Test WF", null, Guid.NewGuid());
        def.AddStep("Step Alpha", null);
        def.AddStep("Step Beta",  null);
        def.Activate(Guid.NewGuid());
        def.ClearDomainEvents();

        var instance = WorkflowInstance.Start(def, Guid.NewGuid());
        instance.ClearDomainEvents();
        return instance;
    }
}
