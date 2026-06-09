using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Teams.Application.Abstractions;
using STRIDE.Modules.Teams.Application.DTOs;
using STRIDE.Modules.Teams.Application.Queries.GetTeams;

namespace STRIDE.Modules.Teams.Application.Tests;

public sealed class GetTeamsQueryHandlerTests
{
    private readonly ITeamReadService _read = Substitute.For<ITeamReadService>();

    private GetTeamsQueryHandler Sut() => new(_read);

    private static PagedResult<TeamSummaryDto> EmptyPage() =>
        PagedResult<TeamSummaryDto>.Empty(1, 20);

    private static TeamSummaryDto SampleSummary() =>
        new(Guid.NewGuid(), "Engineering", null, null, null, 0, "Active", DateTime.UtcNow);

    // ── Success path ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ReturnsSuccessResult()
    {
        var tenantId = Guid.NewGuid();
        _read.GetTeamsAsync(tenantId, null, null, 1, 20, Arg.Any<CancellationToken>())
             .Returns(EmptyPage());

        var result = await Sut().Handle(
            new GetTeamsQuery(tenantId, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ReturnsDelegatedPagedResult()
    {
        var tenantId = Guid.NewGuid();
        var summary  = SampleSummary();
        var page     = new PagedResult<TeamSummaryDto>([summary], 1, 1, 20);
        _read.GetTeamsAsync(tenantId, null, null, 1, 20, Arg.Any<CancellationToken>()).Returns(page);

        var result = await Sut().Handle(
            new GetTeamsQuery(tenantId, null, null), CancellationToken.None);

        result.Value.Items.Should().ContainSingle(t => t.Id == summary.Id);
    }

    [Fact]
    public async Task Handle_PassesSearchFilterToReadService()
    {
        var tenantId = Guid.NewGuid();
        _read.GetTeamsAsync(tenantId, "eng", null, 1, 20, Arg.Any<CancellationToken>())
             .Returns(EmptyPage());

        await Sut().Handle(
            new GetTeamsQuery(tenantId, "eng", null), CancellationToken.None);

        await _read.Received(1).GetTeamsAsync(
            tenantId, "eng", null, 1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesStatusFilterToReadService()
    {
        var tenantId = Guid.NewGuid();
        _read.GetTeamsAsync(tenantId, null, 1, 1, 20, Arg.Any<CancellationToken>())
             .Returns(EmptyPage());

        await Sut().Handle(
            new GetTeamsQuery(tenantId, null, 1), CancellationToken.None);

        await _read.Received(1).GetTeamsAsync(
            tenantId, null, 1, 1, 20, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesPaginationToReadService()
    {
        var tenantId = Guid.NewGuid();
        _read.GetTeamsAsync(tenantId, null, null, 3, 10, Arg.Any<CancellationToken>())
             .Returns(EmptyPage());

        await Sut().Handle(
            new GetTeamsQuery(tenantId, null, null, 3, 10), CancellationToken.None);

        await _read.Received(1).GetTeamsAsync(
            tenantId, null, null, 3, 10, Arg.Any<CancellationToken>());
    }
}
