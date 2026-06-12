using Microsoft.Extensions.Logging.Abstractions;
using STRIDE.Modules.Reporting.Application.Abstractions;
using STRIDE.Modules.Reporting.Application.Queries.GetTeamWorkload;
using STRIDE.Modules.Reporting.Application.ReadModels;

namespace STRIDE.Modules.Reporting.Application.Tests.Queries;

public sealed class GetTeamWorkloadQueryHandlerTests
{
    private readonly IReportingReadService _readService =
        Substitute.For<IReportingReadService>();

    private readonly GetTeamWorkloadQueryHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();

    public GetTeamWorkloadQueryHandlerTests()
    {
        _sut = new GetTeamWorkloadQueryHandler(
            _readService,
            NullLogger<GetTeamWorkloadQueryHandler>.Instance);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WithWorkloadFromReadService()
    {
        // Arrange
        var expected = BuildWorkload();
        _readService.GetTeamWorkloadAsync(TenantId, Arg.Any<CancellationToken>())
                    .Returns(expected);

        // Act
        var result = await _sut.Handle(new GetTeamWorkloadQuery(TenantId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task Handle_PassesTenantIdToReadService()
    {
        // Arrange
        _readService.GetTeamWorkloadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                    .Returns(BuildWorkload());

        // Act
        await _sut.Handle(new GetTeamWorkloadQuery(TenantId), CancellationToken.None);

        // Assert
        await _readService.Received(1)
            .GetTeamWorkloadAsync(TenantId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoUsers_ReturnsEmptyList()
    {
        // Arrange
        _readService.GetTeamWorkloadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                    .Returns(Array.Empty<TeamWorkloadItemDto>());

        // Act
        var result = await _sut.Handle(new GetTeamWorkloadQuery(TenantId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenUsersHaveSteps_ReturnsAllMembers()
    {
        // Arrange
        var workload = new List<TeamWorkloadItemDto>
        {
            new(
                UserId:          Guid.NewGuid(),
                DisplayName:     "Alice Smith",
                Email:           "alice@example.com",
                ActiveStepCount: 3,
                HasOverdueSteps: true,
                TopSteps: new List<TeamMemberStepDto>
                {
                    new(Guid.NewGuid(), "Review", Guid.NewGuid(), "Job Alpha",
                        DueAt: DateTime.UtcNow.AddHours(-2), IsOverdue: true),
                    new(Guid.NewGuid(), "Approve", Guid.NewGuid(), "Job Beta",
                        DueAt: DateTime.UtcNow.AddHours(1), IsOverdue: false),
                }),
            new(
                UserId:          Guid.NewGuid(),
                DisplayName:     "Bob Jones",
                Email:           "bob@example.com",
                ActiveStepCount: 0,
                HasOverdueSteps: false,
                TopSteps:        Array.Empty<TeamMemberStepDto>()),
        };

        _readService.GetTeamWorkloadAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                    .Returns(workload);

        // Act
        var result = await _sut.Handle(new GetTeamWorkloadQuery(TenantId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].DisplayName.Should().Be("Alice Smith");
        result.Value[0].ActiveStepCount.Should().Be(3);
        result.Value[0].HasOverdueSteps.Should().BeTrue();
        result.Value[0].TopSteps.Should().HaveCount(2);
        result.Value[1].ActiveStepCount.Should().Be(0);
        result.Value[1].TopSteps.Should().BeEmpty();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static IReadOnlyList<TeamWorkloadItemDto> BuildWorkload() =>
        Array.Empty<TeamWorkloadItemDto>();
}
