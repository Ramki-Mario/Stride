using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.Commands.RevokeSharedLink;
using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Application.Tests;

public sealed class RevokeSharedLinkCommandHandlerTests
{
    private readonly ISharedWorkflowLinkRepository _links = Substitute.For<ISharedWorkflowLinkRepository>();
    private readonly RevokeSharedLinkCommandHandler _sut;

    public RevokeSharedLinkCommandHandlerTests()
        => _sut = new RevokeSharedLinkCommandHandler(_links);

    [Fact]
    public async Task Handle_WhenLinkNotFound_ReturnsFailure()
    {
        _links.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
              .Returns((SharedWorkflowLink?)null);

        var result = await _sut.Handle(
            new RevokeSharedLinkCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WhenLinkBelongsToDifferentInstance_ReturnsFailure()
    {
        var instanceId = Guid.NewGuid();
        var link = SharedWorkflowLink.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()); // different instance
        _links.GetByIdAsync(link.Id, Arg.Any<CancellationToken>()).Returns(link);

        var result = await _sut.Handle(
            new RevokeSharedLinkCommand(instanceId, link.Id),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        link.IsRevoked.Should().BeFalse();
        await _links.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnSuccess_RevokesAndSaves()
    {
        var instanceId = Guid.NewGuid();
        var link = SharedWorkflowLink.Create(instanceId, Guid.NewGuid(), Guid.NewGuid());
        _links.GetByIdAsync(link.Id, Arg.Any<CancellationToken>()).Returns(link);

        var result = await _sut.Handle(
            new RevokeSharedLinkCommand(instanceId, link.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        link.IsRevoked.Should().BeTrue();
        _links.Received(1).Update(link);
        await _links.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
