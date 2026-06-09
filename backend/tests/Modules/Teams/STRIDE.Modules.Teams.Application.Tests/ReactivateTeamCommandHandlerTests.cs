using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Teams.Application.Commands.ReactivateTeam;
using STRIDE.Modules.Teams.Domain.Entities;
using STRIDE.Modules.Teams.Domain.Enums;
using STRIDE.Modules.Teams.Domain.Repositories;

namespace STRIDE.Modules.Teams.Application.Tests;

public sealed class ReactivateTeamCommandHandlerTests
{
    private readonly ITeamRepository _repo        = Substitute.For<ITeamRepository>();
    private readonly IAuditLogger    _audit       = Substitute.For<IAuditLogger>();
    private readonly ICurrentUser    _currentUser = Substitute.For<ICurrentUser>();

    private ReactivateTeamCommandHandler Sut() => new(_repo, _audit, _currentUser);

    private static Team ActiveTeam(Guid tenantId)
        => Team.Create(new NewTeam(tenantId, "Engineering", null, null, Guid.NewGuid()));

    private static Team InactiveTeam(Guid tenantId)
    {
        var team = ActiveTeam(tenantId);
        team.Deactivate(Guid.NewGuid());
        return team;
    }

    // ── Success path ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_InactiveTeam_ReturnsSuccess()
    {
        var tenantId = Guid.NewGuid();
        var team     = InactiveTeam(tenantId);
        _repo.GetByIdAsync(tenantId, team.Id, Arg.Any<CancellationToken>()).Returns(team);

        var result = await Sut().Handle(new ReactivateTeamCommand(tenantId, team.Id, Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        team.Status.Should().Be(TeamStatus.Active);
    }

    [Fact]
    public async Task Handle_InactiveTeam_SavesChanges()
    {
        var tenantId = Guid.NewGuid();
        var team     = InactiveTeam(tenantId);
        _repo.GetByIdAsync(tenantId, team.Id, Arg.Any<CancellationToken>()).Returns(team);

        await Sut().Handle(new ReactivateTeamCommand(tenantId, team.Id, Guid.NewGuid()), CancellationToken.None);

        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InactiveTeam_FiresAuditLog()
    {
        var tenantId = Guid.NewGuid();
        var team     = InactiveTeam(tenantId);
        _repo.GetByIdAsync(tenantId, team.Id, Arg.Any<CancellationToken>()).Returns(team);

        await Sut().Handle(new ReactivateTeamCommand(tenantId, team.Id, Guid.NewGuid()), CancellationToken.None);

        _ = _audit.Received(1).LogAsync(Arg.Is<AuditLogEntry>(e => e.Action == AuditActions.TeamReactivated));
    }

    // ── Not found ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_TeamNotFound_ReturnsFailure()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
             .Returns((Team?)null);

        var result = await Sut().Handle(
            new ReactivateTeamCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_TeamNotFound_DoesNotSave()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
             .Returns((Team?)null);

        await Sut().Handle(
            new ReactivateTeamCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        await _repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ── Already active ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_AlreadyActive_ReturnsFailure()
    {
        var tenantId = Guid.NewGuid();
        var team     = ActiveTeam(tenantId);
        _repo.GetByIdAsync(tenantId, team.Id, Arg.Any<CancellationToken>()).Returns(team);

        var result = await Sut().Handle(new ReactivateTeamCommand(tenantId, team.Id, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("active");
    }

    [Fact]
    public async Task Handle_AlreadyActive_DoesNotSave()
    {
        var tenantId = Guid.NewGuid();
        var team     = ActiveTeam(tenantId);
        _repo.GetByIdAsync(tenantId, team.Id, Arg.Any<CancellationToken>()).Returns(team);

        await Sut().Handle(new ReactivateTeamCommand(tenantId, team.Id, Guid.NewGuid()), CancellationToken.None);

        await _repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
