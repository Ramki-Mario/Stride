using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.KitOps.Application.Commands.CreateKitItem;
using STRIDE.Modules.KitOps.Domain.Repositories;

namespace STRIDE.Modules.KitOps.Application.Tests.Commands;

public sealed class CreateKitItemCommandHandlerTests
{
    private readonly IKitItemRepository _repo        = Substitute.For<IKitItemRepository>();
    private readonly IAuditLogger       _audit       = Substitute.For<IAuditLogger>();
    private readonly ICurrentUser       _currentUser = Substitute.For<ICurrentUser>();
    private readonly CreateKitItemCommandHandler _handler;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId   = Guid.NewGuid();

    public CreateKitItemCommandHandlerTests()
    {
        _currentUser.Email.Returns("admin@test.com");
        _handler = new CreateKitItemCommandHandler(_repo, _audit, _currentUser);
    }

    private CreateKitItemCommand ValidCommand(string name = "Radio HT1000") =>
        new(TenantId, name, "Communications", "Motorola handheld radio", 10, UserId);

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithId()
    {
        _repo.ExistsByNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
             .Returns(false);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        await _repo.Received(1).AddAsync(Arg.Any<Domain.Entities.KitItem>(), Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateName_ReturnsFailure()
    {
        _repo.ExistsByNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
             .Returns(true);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
        await _repo.DidNotReceive().AddAsync(Arg.Any<Domain.Entities.KitItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ZeroQuantity_ReturnsFailure()
    {
        _repo.ExistsByNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
             .Returns(false);

        var cmd = ValidCommand() with { TotalQuantity = 0 };
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("greater than zero");
    }

    [Fact]
    public async Task Handle_EmptyName_ReturnsFailure()
    {
        _repo.ExistsByNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
             .Returns(false);

        var cmd = ValidCommand("   ");
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("cannot be empty");
    }
}
