using Microsoft.Extensions.Logging.Abstractions;
using STRIDE.Modules.Reporting.Application.Abstractions;
using STRIDE.Modules.Reporting.Application.Queries.GetTeamPerformance;
using STRIDE.Modules.Reporting.Application.ReadModels;

namespace STRIDE.Modules.Reporting.Application.Tests.Queries;

public sealed class GetTeamPerformanceQueryHandlerTests
{
    private readonly IAnalyticsReadService _readService =
        Substitute.For<IAnalyticsReadService>();

    private readonly GetTeamPerformanceQueryHandler _sut;

    private static readonly Guid     TenantId = Guid.NewGuid();
    private static readonly DateTime From     = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime To       = new(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc);

    public GetTeamPerformanceQueryHandlerTests()
    {
        _sut = new GetTeamPerformanceQueryHandler(
            _readService,
            NullLogger<GetTeamPerformanceQueryHandler>.Instance);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WithDataFromReadService()
    {
        // Arrange
        var expected = BuildPerformanceList();
        _readService
            .GetTeamPerformanceAsync(TenantId, From, To, null, Arg.Any<CancellationToken>())
            .Returns(expected);

        // Act
        var result = await _sut.Handle(
            new GetTeamPerformanceQuery(TenantId, From, To), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task Handle_PassesAllParametersToReadService()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        _readService
            .GetTeamPerformanceAsync(
                Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
                Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(BuildPerformanceList());

        // Act
        await _sut.Handle(
            new GetTeamPerformanceQuery(TenantId, From, To, roleId),
            CancellationToken.None);

        // Assert
        await _readService.Received(1).GetTeamPerformanceAsync(
            TenantId, From, To, roleId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNoMembers_ReturnsEmptyList()
    {
        // Arrange
        _readService
            .GetTeamPerformanceAsync(
                Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
                Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TeamMemberPerformanceDto>());

        // Act
        var result = await _sut.Handle(
            new GetTeamPerformanceQuery(TenantId, From, To), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithMembers_ReturnsAllRows()
    {
        // Arrange
        var members = new List<TeamMemberPerformanceDto>
        {
            new(Guid.NewGuid(), "Alice Smith", "alice@example.com", "Manager",
                CompletedSteps: 20, CompletedWorkflows: 5, AvgStepDurationMinutes: 45.0, OverdueRate: 10.0),
            new(Guid.NewGuid(), "Bob Jones",  "bob@example.com",   "Analyst",
                CompletedSteps: 15, CompletedWorkflows: 3, AvgStepDurationMinutes: 60.0, OverdueRate: 0.0),
        };

        _readService
            .GetTeamPerformanceAsync(
                Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
                Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(members);

        // Act
        var result = await _sut.Handle(
            new GetTeamPerformanceQuery(TenantId, From, To), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].DisplayName.Should().Be("Alice Smith");
        result.Value[0].CompletedSteps.Should().Be(20);
        result.Value[1].OverdueRate.Should().Be(0.0);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static IReadOnlyList<TeamMemberPerformanceDto> BuildPerformanceList() =>
        Array.Empty<TeamMemberPerformanceDto>();
}
