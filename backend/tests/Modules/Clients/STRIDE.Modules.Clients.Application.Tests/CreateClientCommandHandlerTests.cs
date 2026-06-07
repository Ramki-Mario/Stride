using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Clients.Application.Commands.CreateClient;
using STRIDE.Modules.Clients.Domain.Entities;
using STRIDE.Modules.Clients.Domain.Repositories;

namespace STRIDE.Modules.Clients.Application.Tests;

public sealed class CreateClientCommandHandlerTests
{
    private readonly IClientRepository _repo        = Substitute.For<IClientRepository>();
    private readonly IAuditLogger       _audit       = Substitute.For<IAuditLogger>();
    private readonly ICurrentUser       _currentUser = Substitute.For<ICurrentUser>();

    private readonly CreateClientCommandHandler _sut;

    public CreateClientCommandHandlerTests()
    {
        _currentUser.Email.Returns("actor@company.com");
        _sut = new CreateClientCommandHandler(_repo, _audit, _currentUser);
    }

    [Fact]
    public async Task Handle_WithUniqueName_ReturnsSuccessWithClientId()
    {
        // Arrange
        _repo.ExistsByNameAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _sut.Handle(BuildCommand("Acme Ltd"), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_WithUniqueName_PersistsClientToRepository()
    {
        // Arrange
        _repo.ExistsByNameAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        await _sut.Handle(BuildCommand("Acme Ltd"), CancellationToken.None);

        // Assert
        await _repo.Received(1).AddAsync(Arg.Any<Client>(), Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDuplicateName_ReturnsFailure()
    {
        // Arrange — simulate existing client with same name
        _repo.ExistsByNameAsync(Arg.Any<Guid>(), "Acme Ltd", Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _sut.Handle(BuildCommand("Acme Ltd"), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task Handle_WithDuplicateName_DoesNotPersistAnything()
    {
        // Arrange
        _repo.ExistsByNameAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _sut.Handle(BuildCommand("Acme Ltd"), CancellationToken.None);

        // Assert
        await _repo.DidNotReceive().AddAsync(Arg.Any<Client>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnSuccess_CallsAuditLogger()
    {
        // Arrange
        _repo.ExistsByNameAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        await _sut.Handle(BuildCommand("Acme Ltd"), CancellationToken.None);

        // Assert
        await _audit.Received(1).LogAsync(
            Arg.Is<AuditLogEntry>(e =>
                e.Action       == AuditActions.ClientCreated &&
                e.ResourceType == "Client"));
    }

    [Fact]
    public async Task Handle_OnDuplicateName_DoesNotCallAuditLogger()
    {
        // Arrange
        _repo.ExistsByNameAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _sut.Handle(BuildCommand("Acme Ltd"), CancellationToken.None);

        // Assert
        await _audit.DidNotReceive().LogAsync(Arg.Any<AuditLogEntry>());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static CreateClientCommand BuildCommand(string name) =>
        new(
            TenantId:      Guid.NewGuid(),
            Name:          name,
            ContactPerson: "John Smith",
            Email:         "john@acme.com",
            Phone:         "07000000000",
            Address:       "1 High Street",
            Notes:         null,
            CreatedBy:     Guid.NewGuid());
}
