using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.Commands.CreateSharedLink;
using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Application.Tests;

public sealed class CreateSharedLinkCommandHandlerTests
{
    private readonly ISharedWorkflowLinkRepository _links     = Substitute.For<ISharedWorkflowLinkRepository>();
    private readonly IWorkflowInstanceRepository   _instances = Substitute.For<IWorkflowInstanceRepository>();
    private readonly ISharedLinkUrlBuilder         _urlBuilder = Substitute.For<ISharedLinkUrlBuilder>();
    private readonly CreateSharedLinkCommandHandler _sut;

    public CreateSharedLinkCommandHandlerTests()
    {
        _urlBuilder.BuildShareUrl(Arg.Any<string>())
                   .Returns(ci => $"https://app.test/job/{ci.Arg<string>()}");
        _sut = new CreateSharedLinkCommandHandler(_links, _instances, _urlBuilder);
    }

    [Fact]
    public async Task Handle_WhenInstanceNotFound_ReturnsFailureAndDoesNotPersist()
    {
        _instances.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                  .Returns((WorkflowInstance?)null);

        var result = await _sut.Handle(
            new CreateSharedLinkCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
        await _links.DidNotReceive().AddAsync(Arg.Any<SharedWorkflowLink>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnSuccess_PersistsLinkAndReturnsDtoWithUrl()
    {
        var instance = BuildInstance();
        var tenantId = Guid.NewGuid();
        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);

        var result = await _sut.Handle(
            new CreateSharedLinkCommand(tenantId, instance.Id, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().NotBeNullOrWhiteSpace();
        result.Value.ShareUrl.Should().Be($"https://app.test/job/{result.Value.Token}");
        result.Value.IsActive.Should().BeTrue();
        result.Value.ViewCount.Should().Be(0);
        result.Value.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(30), TimeSpan.FromMinutes(1));

        await _links.Received(1).AddAsync(Arg.Any<SharedWorkflowLink>(), Arg.Any<CancellationToken>());
        await _links.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HonorsCustomExpiry()
    {
        var instance = BuildInstance();
        _instances.GetByIdAsync(instance.Id, Arg.Any<CancellationToken>()).Returns(instance);

        var result = await _sut.Handle(
            new CreateSharedLinkCommand(Guid.NewGuid(), instance.Id, Guid.NewGuid(), ExpiryDays: 7),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));
    }

    private static WorkflowInstance BuildInstance()
    {
        var def = WorkflowDefinition.Create(Guid.NewGuid(), "WF", null, Guid.NewGuid());
        def.AddStep("Step A", null);
        def.Activate(Guid.NewGuid());
        def.ClearDomainEvents();
        var instance = WorkflowInstance.Start(def, Guid.NewGuid());
        instance.ClearDomainEvents();
        return instance;
    }
}
