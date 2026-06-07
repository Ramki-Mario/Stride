using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Clients.Application.Commands.UpdateClient;
using STRIDE.Modules.Clients.Domain.Entities;
using STRIDE.Modules.Clients.Domain.Repositories;

namespace STRIDE.Modules.Clients.Application.Tests;

public sealed class UpdateClientCommandHandlerTests
{
    private readonly IClientRepository _repo        = Substitute.For<IClientRepository>();
    private readonly IAuditLogger       _audit       = Substitute.For<IAuditLogger>();
    private readonly ICurrentUser       _currentUser = Substitute.For<ICurrentUser>();

    private readonly UpdateClientCommandHandler _sut;

    public UpdateClientCommandHandlerTests()
    {
        _currentUser.Email.Returns("actor@company.com");
        _sut = new UpdateClientCommandHandler(_repo, _audit, _currentUser);
    }

    [Fact]
    public async Task Handle_WhenClientExists_ReturnsSuccess()
    {
        // Arrange
        var client = BuildClient();
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(client);
        _repo.ExistsByNameAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _sut.Handle(BuildCommand(client.Id), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenClientNotFound_ReturnsFailure()
    {
        // Arrange
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Client?)null);

        // Act
        var result = await _sut.Handle(BuildCommand(Guid.NewGuid()), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WhenNameConflicts_ReturnsFailure()
    {
        // Arrange
        var client = BuildClient();
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(client);
        _repo.ExistsByNameAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _sut.Handle(BuildCommand(client.Id), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task Handle_OnSuccess_CallsAuditLogger()
    {
        // Arrange
        var client = BuildClient();
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(client);
        _repo.ExistsByNameAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        await _sut.Handle(BuildCommand(client.Id), CancellationToken.None);

        // Assert
        await _audit.Received(1).LogAsync(
            Arg.Is<AuditLogEntry>(e =>
                e.Action       == AuditActions.ClientUpdated &&
                e.ResourceType == "Client"));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Client BuildClient()
    {
        var client = Client.Create(new NewClient(
            Guid.NewGuid(), "Original Name", null, null, null, null, null, Guid.NewGuid()));
        client.ClearDomainEvents();
        return client;
    }

    private static UpdateClientCommand BuildCommand(Guid clientId) =>
        new(
            TenantId:      Guid.NewGuid(),
            ClientId:      clientId,
            Name:          "Updated Name",
            ContactPerson: null,
            Email:         null,
            Phone:         null,
            Address:       null,
            Notes:         null,
            UpdatedBy:     Guid.NewGuid());
}
