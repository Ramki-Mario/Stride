using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.KitOps.Application.Commands.ReactivateKitItem;
using STRIDE.Modules.KitOps.Domain.Entities;
using STRIDE.Modules.KitOps.Domain.Repositories;

namespace STRIDE.Modules.KitOps.Application.Tests.Commands;

public sealed class ReactivateKitItemCommandHandlerTests
{
    private readonly IKitItemRepository _repo        = Substitute.For<IKitItemRepository>();
    private readonly IAuditLogger       _audit       = Substitute.For<IAuditLogger>();
    private readonly ICurrentUser       _currentUser = Substitute.For<ICurrentUser>();
    private readonly ReactivateKitItemCommandHandler _handler;

    private static readonly Guid TenantId  = Guid.NewGuid();
    private static readonly Guid KitItemId = Guid.NewGuid();
    private static readonly Guid UserId    = Guid.NewGuid();

    public ReactivateKitItemCommandHandlerTests()
    {
        _currentUser.Email.Returns("admin@test.com");
        _handler = new ReactivateKitItemCommandHandler(_repo, _audit, _currentUser);
    }

    private static KitItem InactiveItem()
    {
        var item = KitItem.Create(new NewKitItem(TenantId, "Radio HT1000", "Communications", null, 5, UserId));
        item.Deactivate(UserId);
        return item;
    }

    [Fact]
    public async Task Handle_InactiveItem_ReactivatesAndReturnsSuccess()
    {
        var item = InactiveItem();
        _repo.GetByIdAsync(KitItemId, Arg.Any<CancellationToken>()).Returns(item);

        var result = await _handler.Handle(
            new ReactivateKitItemCommand(TenantId, KitItemId, UserId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        item.IsActive.Should().BeTrue();
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ItemNotFound_ReturnsFailure()
    {
        _repo.GetByIdAsync(KitItemId, Arg.Any<CancellationToken>()).Returns((KitItem?)null);

        var result = await _handler.Handle(
            new ReactivateKitItemCommand(TenantId, KitItemId, UserId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_AlreadyActive_ReturnsFailure()
    {
        var item = KitItem.Create(new NewKitItem(TenantId, "Radio HT1000", "Communications", null, 5, UserId));
        _repo.GetByIdAsync(KitItemId, Arg.Any<CancellationToken>()).Returns(item);

        var result = await _handler.Handle(
            new ReactivateKitItemCommand(TenantId, KitItemId, UserId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already active");
    }
}
