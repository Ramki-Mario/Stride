using System.Reflection;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.Queries.GetPublicJobView;
using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Application.Tests;

public sealed class GetPublicJobViewQueryHandlerTests
{
    private readonly ISharedWorkflowLinkRepository         _links     = Substitute.For<ISharedWorkflowLinkRepository>();
    private readonly IWorkflowInstanceRepository           _instances = Substitute.For<IWorkflowInstanceRepository>();
    private readonly IPublicInvoiceService                 _invoices  = Substitute.For<IPublicInvoiceService>();
    private readonly GetPublicJobViewQueryHandler          _sut;

    public GetPublicJobViewQueryHandlerTests()
    {
        _sut = new GetPublicJobViewQueryHandler(_links, _instances, _invoices,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<GetPublicJobViewQueryHandler>.Instance);
    }

    // ── Token not found ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenTokenNotFound_ReturnsFailure()
    {
        _links.GetByTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
              .Returns((SharedWorkflowLink?)null);

        var result = await _sut.Handle(new GetPublicJobViewQuery("no-such-token"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("expired or been revoked");
        await _instances.DidNotReceive().GetByIdPublicAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    // ── Expired link ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenLinkExpired_ReturnsFailureWithoutTouchingInstance()
    {
        var link = BuildExpiredLink();
        _links.GetByTokenAsync(link.Token, Arg.Any<CancellationToken>()).Returns(link);

        var result = await _sut.Handle(new GetPublicJobViewQuery(link.Token), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _instances.DidNotReceive().GetByIdPublicAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    // ── Revoked link ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenLinkRevoked_ReturnsFailureWithoutTouchingInstance()
    {
        var link = BuildActiveLink();
        link.Revoke();
        _links.GetByTokenAsync(link.Token, Arg.Any<CancellationToken>()).Returns(link);

        var result = await _sut.Handle(new GetPublicJobViewQuery(link.Token), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        await _instances.DidNotReceive().GetByIdPublicAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    // ── Instance missing (data integrity issue) ────────────────────────────

    [Fact]
    public async Task Handle_WhenInstanceMissing_ReturnsFailure()
    {
        var link = BuildActiveLink();
        _links.GetByTokenAsync(link.Token, Arg.Any<CancellationToken>()).Returns(link);
        _instances.GetByIdPublicAsync(link.WorkflowInstanceId, link.TenantId, Arg.Any<CancellationToken>())
                  .Returns((WorkflowInstance?)null);

        var result = await _sut.Handle(new GetPublicJobViewQuery(link.Token), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _links.DidNotReceive().Update(Arg.Any<SharedWorkflowLink>());
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenLinkValid_ReturnsPublicDtoAndIncrementsViewCount()
    {
        var link     = BuildActiveLink();
        var instance = BuildInstance(link.WorkflowInstanceId, link.TenantId);
        _links.GetByTokenAsync(link.Token, Arg.Any<CancellationToken>()).Returns(link);
        _instances.GetByIdPublicAsync(link.WorkflowInstanceId, link.TenantId, Arg.Any<CancellationToken>())
                  .Returns(instance);

        var viewCountBefore = link.ViewCount;

        var result = await _sut.Handle(new GetPublicJobViewQuery(link.Token), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.WorkflowName.Should().Be(instance.WorkflowName);
        result.Value.Status.Should().NotBeNullOrWhiteSpace();
        result.Value.Steps.Should().NotBeEmpty();
        link.ViewCount.Should().Be(viewCountBefore + 1);

        _links.Received(1).Update(link);
        await _links.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenLinkValid_DtoExcludesBillableAndAssigneeData()
    {
        var link     = BuildActiveLink();
        var instance = BuildInstance(link.WorkflowInstanceId, link.TenantId);
        _links.GetByTokenAsync(link.Token, Arg.Any<CancellationToken>()).Returns(link);
        _instances.GetByIdPublicAsync(link.WorkflowInstanceId, link.TenantId, Arg.Any<CancellationToken>())
                  .Returns(instance);

        var result = await _sut.Handle(new GetPublicJobViewQuery(link.Token), CancellationToken.None);

        // PublicJobViewDto must not expose the type's full reflected surface —
        // just verify the DTO type only has the expected public properties.
        var props = typeof(PublicJobViewDto).GetProperties().Select(p => p.Name).ToHashSet();
        props.Should().NotContain("BillableTotal");
        props.Should().NotContain("AssigneeId");
        props.Should().NotContain("TenantId");
        props.Should().NotContain("StartedBy");
        props.Should().NotContain("FailureReason");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SharedWorkflowLink BuildActiveLink()
        => SharedWorkflowLink.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), expiryDays: 30);

    private static SharedWorkflowLink BuildExpiredLink()
    {
        var link = BuildActiveLink();
        // Backdoor ExpiresAt via reflection — public API only allows positive days.
        typeof(SharedWorkflowLink)
            .GetProperty(nameof(SharedWorkflowLink.ExpiresAt))!
            .SetValue(link, DateTime.UtcNow.AddDays(-1));
        return link;
    }

    private static WorkflowInstance BuildInstance(Guid instanceId, Guid tenantId)
    {
        var def = WorkflowDefinition.Create(tenantId, "Public Job Test", null, Guid.NewGuid());
        def.AddStep("Document Review", null);
        def.AddStep("Approval", null);
        def.Activate(Guid.NewGuid());
        def.ClearDomainEvents();

        var instance = WorkflowInstance.Start(def, Guid.NewGuid());
        instance.ClearDomainEvents();

        // Force the instance ID to match the link's WorkflowInstanceId.
        typeof(WorkflowInstance).BaseType!
            .GetProperty("Id")!
            .SetValue(instance, instanceId);

        return instance;
    }
}
