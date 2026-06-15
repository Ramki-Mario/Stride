using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.KitOps.Application.Commands.UpdateKitItem;
using STRIDE.Modules.KitOps.Domain.Entities;
using STRIDE.Modules.KitOps.Domain.Repositories;

namespace STRIDE.Modules.KitOps.Application.Tests.Commands;

public sealed class UpdateKitItemCommandHandlerTests
{
    private readonly IKitItemRepository _repo        = Substitute.For<IKitItemRepository>();
    private readonly IAuditLogger       _audit       = Substitute.For<IAuditLogger>();
    private readonly ICurrentUser       _currentUser = Substitute.For<ICurrentUser>();
    private readonly UpdateKitItemCommandHandler _handler;

    private static readonly Guid TenantId   = Guid.NewGuid();
    private static readonly Guid KitItemId  = Guid.NewGuid();
    private static readonly Guid UserId     = Guid.NewGuid();

    public UpdateKitItemCommandHandlerTests()
    {
        _currentUser.Email.Returns("admin@test.com");
        _handler = new UpdateKitItemCommandHandler(_repo, _audit, _currentUser);
    }

    private static KitItem ActiveItem() =>
        KitItem.Create(new NewKitItem(TenantId, "Radio HT1000", "Communications", null, 5, UserId));

    private UpdateKitItemCommand ValidCommand() =>
        new(TenantId, KitItemId, "Radio HT2000", "Communications", null, 8, UserId);

    [Fact]
    public async Task Handle_ItemExists_UpdatesAndReturnsSuccess()
    {
        var item = ActiveItem();
        _repo.GetByIdAsync(KitItemId, Arg.Any<CancellationToken>()).Returns(item);
        _repo.ExistsByNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
             .Returns(false);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        item.Name.Should().Be("Radio HT2000");
        item.TotalQuantity.Should().Be(8);
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ItemNotFound_ReturnsFailure()
    {
        _repo.GetByIdAsync(KitItemId, Arg.Any<CancellationToken>()).Returns((KitItem?)null);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_DuplicateName_ReturnsFailure()
    {
        var item = ActiveItem();
        _repo.GetByIdAsync(KitItemId, Arg.Any<CancellationToken>()).Returns(item);
        _repo.ExistsByNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
             .Returns(true);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }
}
