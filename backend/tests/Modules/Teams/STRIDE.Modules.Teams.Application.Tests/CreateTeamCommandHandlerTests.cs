using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Teams.Application.Commands.CreateTeam;
using STRIDE.Modules.Teams.Domain.Entities;
using STRIDE.Modules.Teams.Domain.Repositories;

namespace STRIDE.Modules.Teams.Application.Tests;

public sealed class CreateTeamCommandHandlerTests
{
    private readonly ITeamRepository _repo        = Substitute.For<ITeamRepository>();
    private readonly IAuditLogger    _audit       = Substitute.For<IAuditLogger>();
    private readonly ICurrentUser    _currentUser = Substitute.For<ICurrentUser>();

    private CreateTeamCommandHandler Sut() => new(_repo, _audit, _currentUser);

    private static CreateTeamCommand ValidCommand(Guid? tenantId = null) =>
        new(tenantId ?? Guid.NewGuid(), "Engineering", "Core team", null, Guid.NewGuid());

    // ── Success path ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithNewGuid()
    {
        _repo.ExistsByNameAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
             .Returns(false);

        var result = await Sut().Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsAddAndSave()
    {
        _repo.ExistsByNameAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
             .Returns(false);

        await Sut().Handle(ValidCommand(), CancellationToken.None);

        await _repo.Received(1).AddAsync(Arg.Any<Team>(), Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommand_FiresAuditLog()
    {
        _repo.ExistsByNameAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
             .Returns(false);

        await Sut().Handle(ValidCommand(), CancellationToken.None);

        _ = _audit.Received(1).LogAsync(Arg.Is<AuditLogEntry>(e => e.Action == AuditActions.TeamCreated));
    }

    // ── Duplicate name ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_DuplicateName_ReturnsFailure()
    {
        _repo.ExistsByNameAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
             .Returns(true);

        var result = await Sut().Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public async Task Handle_DuplicateName_DoesNotCallAddOrSave()
    {
        _repo.ExistsByNameAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
             .Returns(true);

        await Sut().Handle(ValidCommand(), CancellationToken.None);

        await _repo.DidNotReceive().AddAsync(Arg.Any<Team>(), Arg.Any<CancellationToken>());
        await _repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
