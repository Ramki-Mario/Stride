using FluentAssertions;
using NSubstitute;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Administration.Application.Abstractions;
using STRIDE.Modules.Administration.Application.Commands.DeactivateUser;

namespace STRIDE.Modules.Administration.Application.Tests;

public sealed class DeactivateUserCommandHandlerTests
{
    private readonly IAdminWriteService _writeService = Substitute.For<IAdminWriteService>();
    private readonly IAuditLogger       _audit        = Substitute.For<IAuditLogger>();
    private readonly ICurrentUser       _currentUser  = Substitute.For<ICurrentUser>();

    private readonly DeactivateUserCommandHandler _sut;

    public DeactivateUserCommandHandlerTests()
    {
        _currentUser.Email.Returns("admin@company.com");
        _sut = new DeactivateUserCommandHandler(_writeService, _audit, _currentUser);
    }

    [Fact]
    public async Task Handle_WhenCallerDeactivatesOtherUser_Succeeds()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId   = Guid.NewGuid();
        var callerId = Guid.NewGuid(); // different user
        var command  = new DeactivateUserCommand(tenantId, userId, callerId);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _writeService.Received(1)
            .DeactivateUserAsync(tenantId, userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCallerDeactivatesThemself_ReturnsFailure()
    {
        // Arrange
        var selfId  = Guid.NewGuid();
        var command = new DeactivateUserCommand(Guid.NewGuid(), selfId, selfId);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("cannot deactivate your own account");
    }

    [Fact]
    public async Task Handle_WhenCallerDeactivatesThemself_DoesNotCallWriteService()
    {
        // Arrange
        var selfId  = Guid.NewGuid();
        var command = new DeactivateUserCommand(Guid.NewGuid(), selfId, selfId);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _writeService.DidNotReceive()
            .DeactivateUserAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OnSuccessfulDeactivation_CallsAuditLogger()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId   = Guid.NewGuid();
        var command  = new DeactivateUserCommand(tenantId, userId, Guid.NewGuid());

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _audit.Received(1).LogAsync(
            Arg.Is<AuditLogEntry>(e =>
                e.TenantId     == tenantId              &&
                e.Action       == AuditActions.UserDeactivated &&
                e.ResourceType == "User"                &&
                e.ResourceId   == userId));
    }

    [Fact]
    public async Task Handle_WhenSelfDeactivation_DoesNotCallAuditLogger()
    {
        // Arrange
        var selfId  = Guid.NewGuid();
        var command = new DeactivateUserCommand(Guid.NewGuid(), selfId, selfId);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _audit.DidNotReceive().LogAsync(Arg.Any<AuditLogEntry>());
    }
}
