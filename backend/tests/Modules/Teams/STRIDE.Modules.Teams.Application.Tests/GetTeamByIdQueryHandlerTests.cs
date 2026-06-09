using STRIDE.Modules.Teams.Application.Abstractions;
using STRIDE.Modules.Teams.Application.DTOs;
using STRIDE.Modules.Teams.Application.Queries.GetTeamById;

namespace STRIDE.Modules.Teams.Application.Tests;

public sealed class GetTeamByIdQueryHandlerTests
{
    private readonly ITeamReadService _read = Substitute.For<ITeamReadService>();

    private GetTeamByIdQueryHandler Sut() => new(_read);

    private static TeamDetailDto SampleDetail(Guid id, Guid tenantId) =>
        new(id, "Engineering", null, null, null, 0, "Active", DateTime.UtcNow, DateTime.UtcNow);

    // ── Found ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_TeamExists_ReturnsSuccessWithDto()
    {
        var tenantId = Guid.NewGuid();
        var teamId   = Guid.NewGuid();
        var dto      = SampleDetail(teamId, tenantId);
        _read.GetTeamByIdAsync(tenantId, teamId, Arg.Any<CancellationToken>()).Returns(dto);

        var result = await Sut().Handle(
            new GetTeamByIdQuery(tenantId, teamId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(teamId);
        result.Value.Name.Should().Be("Engineering");
    }

    [Fact]
    public async Task Handle_TeamExists_DelegatesToReadService()
    {
        var tenantId = Guid.NewGuid();
        var teamId   = Guid.NewGuid();
        _read.GetTeamByIdAsync(tenantId, teamId, Arg.Any<CancellationToken>())
             .Returns(SampleDetail(teamId, tenantId));

        await Sut().Handle(new GetTeamByIdQuery(tenantId, teamId), CancellationToken.None);

        await _read.Received(1).GetTeamByIdAsync(tenantId, teamId, Arg.Any<CancellationToken>());
    }

    // ── Not found ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_TeamNotFound_ReturnsFailure()
    {
        _read.GetTeamByIdAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
             .Returns((TeamDetailDto?)null);

        var result = await Sut().Handle(
            new GetTeamByIdQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_TeamNotFound_ErrorContainsTeamId()
    {
        var teamId = Guid.NewGuid();
        _read.GetTeamByIdAsync(Arg.Any<Guid>(), teamId, Arg.Any<CancellationToken>())
             .Returns((TeamDetailDto?)null);

        var result = await Sut().Handle(
            new GetTeamByIdQuery(Guid.NewGuid(), teamId), CancellationToken.None);

        result.Error.Should().Contain(teamId.ToString());
    }
}
