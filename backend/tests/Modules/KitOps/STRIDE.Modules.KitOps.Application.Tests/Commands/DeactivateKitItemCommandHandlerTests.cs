using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.KitOps.Application.Commands.DeactivateKitItem;
using STRIDE.Modules.KitOps.Domain.Entities;
using STRIDE.Modules.KitOps.Domain.Repositories;

namespace STRIDE.Modules.KitOps.Application.Tests.Commands;

public sealed class DeactivateKitItemCommandHandlerTests
{
    private readonly IKitItemRepository _repo        = Substitute.For<IKitItemRepository>();
    private readonly IAuditLogger       _audit       = Substitute.For<IAuditLogger>();
    private readonly ICurrentUser       _currentUser = Substitute.For<ICurrentUser>();
    private readonly DeactivateKitItemCommandHandler _handler;

    private static readonly Guid TenantId  = Guid.NewGuid();
    private static readonly Guid KitItemId = Guid.NewGuid();
    private static readonly Guid UserId    = Guid.NewGuid();

    public DeactivateKitItemCommandHandlerTests()
    {
        _currentUser.Email.Returns("admin@test.com");
        _handler = new DeactivateKitItemCommandHandler(_repo, _audit, _currentUser);
    }

    private static KitItem ActiveItem() =>
        KitItem.Create(new NewKitItem(TenantId, "Radio HT1000", "Communications", null, 5, UserId));

    [Fact]
    public async Task Handle_ActiveItem_DeactivatesAndReturnsSuccess()
    {
        var item = ActiveItem();
        _repo.GetByIdAsync(KitItemId, Arg.Any<CancellationToken>()).Returns(item);

        var result = await _handler.Handle(
            new DeactivateKitItemCommand(TenantId, KitItemId, UserId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        item.IsActive.Should().BeFalse();
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ItemNotFound_ReturnsFailure()
    {
        _repo.GetByIdAsync(KitItemId, Arg.Any<CancellationToken>()).Returns((KitItem?)null);

        var result = await _handler.Handle(
            new DeactivateKitItemCommand(TenantId, KitItemId, UserId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_AlreadyInactive_ReturnsFailure()
    {
        var item = ActiveItem();
        item.Deactivate(UserId);
        _repo.GetByIdAsync(KitItemId, Arg.Any<CancellationToken>()).Returns(item);

        var result = await _handler.Handle(
            new DeactivateKitItemCommand(TenantId, KitItemId, UserId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already inactive");
    }
}
