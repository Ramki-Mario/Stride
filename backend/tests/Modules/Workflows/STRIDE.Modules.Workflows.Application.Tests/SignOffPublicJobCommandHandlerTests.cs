using System.Reflection;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.Commands.SignOffPublicJob;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Enums;

namespace STRIDE.Modules.Workflows.Application.Tests;

public sealed class SignOffPublicJobCommandHandlerTests
{
    private readonly ISharedWorkflowLinkRepository              _links     = Substitute.For<ISharedWorkflowLinkRepository>();
    private readonly IWorkflowInstanceRepository                _instances = Substitute.For<IWorkflowInstanceRepository>();
    private readonly SignOffPublicJobCommandHandler             _sut;

    public SignOffPublicJobCommandHandlerTests()
    {
        _sut = new SignOffPublicJobCommandHandler(_links, _instances,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<SignOffPublicJobCommandHandler>.Instance);
    }

    // ── Token not found ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenTokenNotFound_ReturnsFailure()
    {
        _links.GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Returns((SharedWorkflowLink?)null);

        var result = await _sut.Handle(new SignOffPublicJobCommand("no-token", "Alice"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("expired or been revoked");
        _links.DidNotReceive().Update(Arg.Any<SharedWorkflowLink>());
    }

    // ── Expired link ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenLinkExpired_ReturnsFailure()
    {
        var link = BuildExpiredLink();
        _links.GetByTokenAsync(link.Token, Arg.Any<CancellationToken>()).Returns(link);

        var result = await _sut.Handle(new SignOffPublicJobCommand(link.Token, "Alice"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("expired or been revoked");
    }

    // ── Revoked link ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenLinkRevoked_ReturnsFailure()
    {
        var link = BuildActiveLink();
        link.Revoke();
        _links.GetByTokenAsync(link.Token, Arg.Any<CancellationToken>()).Returns(link);

        var result = await _sut.Handle(new SignOffPublicJobCommand(link.Token, "Alice"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("expired or been revoked");
    }

    // ── Instance missing ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenInstanceMissing_ReturnsFailure()
    {
        var link = BuildActiveLink();
        _links.GetByTokenAsync(link.Token, Arg.Any<CancellationToken>()).Returns(link);
        _instances.GetByIdPublicAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                  .Returns((WorkflowInstance?)null);

        var result = await _sut.Handle(new SignOffPublicJobCommand(link.Token, "Alice"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("expired or been revoked");
    }

    // ── Instance not yet completed ────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenInstanceNotCompleted_ReturnsBadRequest()
    {
        var link     = BuildActiveLink();
        var instance = BuildInstance(link.WorkflowInstanceId, link.TenantId, WorkflowStatus.Running);
        _links.GetByTokenAsync(link.Token, Arg.Any<CancellationToken>()).Returns(link);
        _instances.GetByIdPublicAsync(link.WorkflowInstanceId, link.TenantId, Arg.Any<CancellationToken>())
                  .Returns(instance);

        var result = await _sut.Handle(new SignOffPublicJobCommand(link.Token, "Alice"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("completed");
        _links.DidNotReceive().Update(Arg.Any<SharedWorkflowLink>());
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenValid_SetsSignOffAndSaves()
    {
        var link     = BuildActiveLink();
        var instance = BuildInstance(link.WorkflowInstanceId, link.TenantId, WorkflowStatus.Completed);
        _links.GetByTokenAsync(link.Token, Arg.Any<CancellationToken>()).Returns(link);
        _instances.GetByIdPublicAsync(link.WorkflowInstanceId, link.TenantId, Arg.Any<CancellationToken>())
                  .Returns(instance);

        var result = await _sut.Handle(new SignOffPublicJobCommand(link.Token, "Alice"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        link.SignedOffAt.Should().NotBeNull();
        link.SignedOffBy.Should().Be("Alice");
        _links.Received(1).Update(link);
        await _links.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Idempotency ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenAlreadySignedOff_IsIdempotent()
    {
        var link     = BuildActiveLink();
        var instance = BuildInstance(link.WorkflowInstanceId, link.TenantId, WorkflowStatus.Completed);
        link.SignOff("Alice");
        var firstSignedOffAt = link.SignedOffAt;

        _links.GetByTokenAsync(link.Token, Arg.Any<CancellationToken>()).Returns(link);
        _instances.GetByIdPublicAsync(link.WorkflowInstanceId, link.TenantId, Arg.Any<CancellationToken>())
                  .Returns(instance);

        var result = await _sut.Handle(new SignOffPublicJobCommand(link.Token, "Bob"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        link.SignedOffBy.Should().Be("Alice");            // not overwritten
        link.SignedOffAt.Should().Be(firstSignedOffAt);  // not overwritten
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SharedWorkflowLink BuildActiveLink()
        => SharedWorkflowLink.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), expiryDays: 30);

    private static SharedWorkflowLink BuildExpiredLink()
    {
        var link = BuildActiveLink();
        typeof(SharedWorkflowLink)
            .GetProperty(nameof(SharedWorkflowLink.ExpiresAt))!
            .SetValue(link, DateTime.UtcNow.AddDays(-1));
        return link;
    }

    private static WorkflowInstance BuildInstance(Guid instanceId, Guid tenantId, WorkflowStatus status)
    {
        var def = WorkflowDefinition.Create(tenantId, "Test Workflow", null, Guid.NewGuid());
        def.AddStep("Step 1", null);
        def.Activate(Guid.NewGuid());
        def.ClearDomainEvents();

        var instance = WorkflowInstance.Start(def, Guid.NewGuid());
        instance.ClearDomainEvents();

        typeof(WorkflowInstance).BaseType!
            .GetProperty("Id")!
            .SetValue(instance, instanceId);

        typeof(WorkflowInstance)
            .GetProperty(nameof(WorkflowInstance.Status))!
            .SetValue(instance, status);

        return instance;
    }
}
