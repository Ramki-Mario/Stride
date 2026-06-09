using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Teams.Application.Commands.UpdateTeam;
using STRIDE.Modules.Teams.Domain.Entities;
using STRIDE.Modules.Teams.Domain.Enums;
using STRIDE.Modules.Teams.Domain.Repositories;

namespace STRIDE.Modules.Teams.Application.Tests;

public sealed class UpdateTeamCommandHandlerTests
{
    private readonly ITeamRepository _repo        = Substitute.For<ITeamRepository>();
    private readonly IAuditLogger    _audit       = Substitute.For<IAuditLogger>();
    private readonly ICurrentUser    _currentUser = Substitute.For<ICurrentUser>();

    private UpdateTeamCommandHandler Sut() => new(_repo, _audit, _currentUser);

    private static Team ValidTeam(Guid tenantId)
        => Team.Create(new NewTeam(tenantId, "Engineering", null, null, Guid.NewGuid()));

    // ── Success path ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccess()
    {
        var tenantId = Guid.NewGuid();
        var team     = ValidTeam(tenantId);

        _repo.GetByIdAsync(tenantId, team.Id, Arg.Any<CancellationToken>()).Returns(team);
        _repo.ExistsByNameAsync(tenantId, "Backend", team.Id, Arg.Any<CancellationToken>()).Returns(false);

        var cmd    = new UpdateTeamCommand(tenantId, team.Id, "Backend", null, null, Guid.NewGuid());
        var result = await Sut().Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        team.Name.Should().Be("Backend");
    }

    [Fact]
    public async Task Handle_ValidCommand_SavesChanges()
    {
        var tenantId = Guid.NewGuid();
        var team     = ValidTeam(tenantId);

        _repo.GetByIdAsync(tenantId, team.Id, Arg.Any<CancellationToken>()).Returns(team);
        _repo.ExistsByNameAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);

        await Sut().Handle(new UpdateTeamCommand(tenantId, team.Id, "Backend", null, null, Guid.NewGuid()), CancellationToken.None);

        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommand_FiresAuditLog()
    {
        var tenantId = Guid.NewGuid();
        var team     = ValidTeam(tenantId);

        _repo.GetByIdAsync(tenantId, team.Id, Arg.Any<CancellationToken>()).Returns(team);
        _repo.ExistsByNameAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);

        await Sut().Handle(new UpdateTeamCommand(tenantId, team.Id, "Backend", null, null, Guid.NewGuid()), CancellationToken.None);

        _ = _audit.Received(1).LogAsync(Arg.Is<AuditLogEntry>(e => e.Action == AuditActions.TeamUpdated));
    }

    // ── Not found ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_TeamNotFound_ReturnsFailure()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Team?)null);

        var cmd    = new UpdateTeamCommand(Guid.NewGuid(), Guid.NewGuid(), "X", null, null, Guid.NewGuid());
        var result = await Sut().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    // ── Duplicate name ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_DuplicateName_ReturnsFailure()
    {
        var tenantId = Guid.NewGuid();
        var team     = ValidTeam(tenantId);

        _repo.GetByIdAsync(tenantId, team.Id, Arg.Any<CancellationToken>()).Returns(team);
        _repo.ExistsByNameAsync(tenantId, "Duplicate", team.Id, Arg.Any<CancellationToken>()).Returns(true);

        var cmd    = new UpdateTeamCommand(tenantId, team.Id, "Duplicate", null, null, Guid.NewGuid());
        var result = await Sut().Handle(cmd, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }

    // ── Deactivated team ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_DeactivatedTeam_UpdateIsAllowed()
    {
        var tenantId = Guid.NewGuid();
        var team     = ValidTeam(tenantId);
        team.Deactivate(Guid.NewGuid());

        _repo.GetByIdAsync(tenantId, team.Id, Arg.Any<CancellationToken>()).Returns(team);
        _repo.ExistsByNameAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(false);

        var cmd    = new UpdateTeamCommand(tenantId, team.Id, "Backend", null, null, Guid.NewGuid());
        var result = await Sut().Handle(cmd, CancellationToken.None);

        // Update is allowed on inactive teams; status should remain Inactive
        result.IsSuccess.Should().BeTrue();
        team.Status.Should().Be(TeamStatus.Inactive);
    }
}
