using STRIDE.Modules.Teams.API.Controllers;

namespace STRIDE.Modules.Teams.API.Tests;

public sealed class TeamsControllerTests
{
    private readonly IMediator       _mediator      = Substitute.For<IMediator>();
    private readonly ICurrentUser    _currentUser   = Substitute.For<ICurrentUser>();
    private readonly ITenantContext  _tenantContext = Substitute.For<ITenantContext>();

    private TeamsController Sut() => new(_mediator, _currentUser, _tenantContext);

    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _userId   = Guid.NewGuid();

    public TeamsControllerTests()
    {
        _tenantContext.TenantId.Returns(_tenantId);
        _currentUser.UserId.Returns(_userId);
    }

    // ── GetTeams ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTeams_ReturnsOk()
    {
        var paged = PagedResult<TeamSummaryDto>.Empty(1, 20);
        _mediator.Send(Arg.Any<GetTeamsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<PagedResult<TeamSummaryDto>>>(Result.Success(paged)));

        var result = await Sut().GetTeams();

        result.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetTeams_PassesTenantIdAndFilters()
    {
        var paged = PagedResult<TeamSummaryDto>.Empty(2, 10);
        _mediator.Send(
            Arg.Is<GetTeamsQuery>(q => q.TenantId == _tenantId && q.Search == "eng" && q.Page == 2),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<PagedResult<TeamSummaryDto>>>(Result.Success(paged)));

        var result = await Sut().GetTeams(page: 2, pageSize: 10, search: "eng", status: null);

        result.Should().BeOfType<OkObjectResult>();
    }

    // ── CreateTeam ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateTeam_Success_Returns201Created()
    {
        var newId = Guid.NewGuid();
        _mediator.Send(Arg.Any<CreateTeamCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(newId)));

        var request = new CreateTeamRequest("Engineering", null, null);
        var result  = await Sut().CreateTeam(request, CancellationToken.None);

        result.Should().BeOfType<CreatedAtActionResult>()
            .Which.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task CreateTeam_Success_BodyContainsNewId()
    {
        var newId = Guid.NewGuid();
        _mediator.Send(Arg.Any<CreateTeamCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(newId)));

        var request = new CreateTeamRequest("Engineering", null, null);
        var result  = (CreatedAtActionResult)(await Sut().CreateTeam(request, CancellationToken.None));

        result.Value.Should().BeEquivalentTo(new { id = newId });
    }

    [Fact]
    public async Task CreateTeam_Failure_Returns400BadRequest()
    {
        _mediator.Send(Arg.Any<CreateTeamCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<Guid>("A team named 'X' already exists.")));

        var request = new CreateTeamRequest("X", null, null);
        var result  = await Sut().CreateTeam(request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.StatusCode.Should().Be(400);
    }

    // ── GetTeam ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTeam_Exists_ReturnsOk()
    {
        var id  = Guid.NewGuid();
        var dto = new TeamDetailDto(id, "Engineering", null, null, null, 0, "Active",
            DateTime.UtcNow, DateTime.UtcNow);

        _mediator.Send(Arg.Any<GetTeamByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<TeamDetailDto>(dto)));

        var result = await Sut().GetTeam(id, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>()
            .Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetTeam_NotFound_Returns404()
    {
        _mediator.Send(Arg.Any<GetTeamByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<TeamDetailDto>("Team 'x' not found.")));

        var result = await Sut().GetTeam(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>()
            .Which.StatusCode.Should().Be(404);
    }

    // ── UpdateTeam ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateTeam_Success_Returns204NoContent()
    {
        _mediator.Send(Arg.Any<UpdateTeamCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        var request = new UpdateTeamRequest("Backend", null, null);
        var result  = await Sut().UpdateTeam(Guid.NewGuid(), request, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>()
            .Which.StatusCode.Should().Be(204);
    }

    [Fact]
    public async Task UpdateTeam_NotFound_Returns404()
    {
        _mediator.Send(Arg.Any<UpdateTeamCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure("Team 'x' not found.")));

        var request = new UpdateTeamRequest("Backend", null, null);
        var result  = await Sut().UpdateTeam(Guid.NewGuid(), request, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>()
            .Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task UpdateTeam_OtherFailure_Returns400()
    {
        _mediator.Send(Arg.Any<UpdateTeamCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure("A team named 'Backend' already exists.")));

        var request = new UpdateTeamRequest("Backend", null, null);
        var result  = await Sut().UpdateTeam(Guid.NewGuid(), request, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.StatusCode.Should().Be(400);
    }

    // ── DeactivateTeam ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateTeam_Success_Returns204NoContent()
    {
        _mediator.Send(Arg.Any<DeactivateTeamCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        var result = await Sut().DeactivateTeam(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>()
            .Which.StatusCode.Should().Be(204);
    }

    [Fact]
    public async Task DeactivateTeam_NotFound_Returns404()
    {
        _mediator.Send(Arg.Any<DeactivateTeamCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure("Team 'x' not found.")));

        var result = await Sut().DeactivateTeam(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>()
            .Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task DeactivateTeam_AlreadyInactive_Returns400()
    {
        _mediator.Send(Arg.Any<DeactivateTeamCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure("Team is already inactive.")));

        var result = await Sut().DeactivateTeam(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.StatusCode.Should().Be(400);
    }

    // ── ReactivateTeam ───────────────────────────────────────────────────────

    [Fact]
    public async Task ReactivateTeam_Success_Returns204NoContent()
    {
        _mediator.Send(Arg.Any<ReactivateTeamCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));

        var result = await Sut().ReactivateTeam(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>()
            .Which.StatusCode.Should().Be(204);
    }

    [Fact]
    public async Task ReactivateTeam_NotFound_Returns404()
    {
        _mediator.Send(Arg.Any<ReactivateTeamCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure("Team 'x' not found.")));

        var result = await Sut().ReactivateTeam(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>()
            .Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task ReactivateTeam_AlreadyActive_Returns400()
    {
        _mediator.Send(Arg.Any<ReactivateTeamCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure("Team is already active.")));

        var result = await Sut().ReactivateTeam(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.StatusCode.Should().Be(400);
    }
}
