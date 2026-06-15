using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.KitOps.Application.Commands.ReturnKitCheckout;
using STRIDE.Modules.KitOps.Domain.Entities;
using STRIDE.Modules.KitOps.Domain.Repositories;

namespace STRIDE.Modules.KitOps.Application.Tests.Commands;

public sealed class ReturnKitCheckoutCommandHandlerTests
{
    private readonly IKitCheckoutRepository _checkouts = Substitute.For<IKitCheckoutRepository>();
    private readonly IAuditLogger           _audit     = Substitute.For<IAuditLogger>();
    private readonly ICurrentUser           _currentUser = Substitute.For<ICurrentUser>();
    private readonly ReturnKitCheckoutCommandHandler _handler;

    private static readonly Guid TenantId   = Guid.NewGuid();
    private static readonly Guid CheckoutId = Guid.NewGuid();
    private static readonly Guid KitItemId  = Guid.NewGuid();
    private static readonly Guid UserId     = Guid.NewGuid();

    public ReturnKitCheckoutCommandHandlerTests()
    {
        _currentUser.Email.Returns("field@test.com");
        _handler = new ReturnKitCheckoutCommandHandler(_checkouts, _audit, _currentUser);
    }

    private static KitCheckout ActiveCheckout() =>
        KitCheckout.Create(new NewKitCheckout(
            TenantId, KitItemId, UserId, DateTime.UtcNow.AddDays(3), null));

    [Fact]
    public async Task Handle_ActiveCheckout_ReturnsAndRestoresAvailability()
    {
        var checkout = ActiveCheckout();
        _checkouts.GetByIdAsync(CheckoutId, Arg.Any<CancellationToken>()).Returns(checkout);

        var result = await _handler.Handle(
            new ReturnKitCheckoutCommand(TenantId, CheckoutId, UserId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        checkout.ReturnedAt.Should().NotBeNull();
        await _checkouts.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CheckoutNotFound_ReturnsFailure()
    {
        _checkouts.GetByIdAsync(CheckoutId, Arg.Any<CancellationToken>()).Returns((KitCheckout?)null);

        var result = await _handler.Handle(
            new ReturnKitCheckoutCommand(TenantId, CheckoutId, UserId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
        await _checkouts.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyReturned_ReturnsFailure()
    {
        var checkout = ActiveCheckout();
        checkout.Return(DateTime.UtcNow, UserId);
        _checkouts.GetByIdAsync(CheckoutId, Arg.Any<CancellationToken>()).Returns(checkout);

        var result = await _handler.Handle(
            new ReturnKitCheckoutCommand(TenantId, CheckoutId, UserId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already been returned");
    }
}
