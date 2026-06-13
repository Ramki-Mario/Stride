using Microsoft.Extensions.Logging.Abstractions;
using STRIDE.Modules.Reporting.Application.Abstractions;
using STRIDE.Modules.Reporting.Application.Queries.GetCompletionTimeAnalytics;
using STRIDE.Modules.Reporting.Application.ReadModels;

namespace STRIDE.Modules.Reporting.Application.Tests.Queries;

public sealed class GetCompletionTimeAnalyticsQueryHandlerTests
{
    private readonly IAnalyticsReadService _readService =
        Substitute.For<IAnalyticsReadService>();

    private readonly GetCompletionTimeAnalyticsQueryHandler _sut;

    private static readonly Guid     TenantId = Guid.NewGuid();
    private static readonly DateTime From     = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime To       = new(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc);

    public GetCompletionTimeAnalyticsQueryHandlerTests()
    {
        _sut = new GetCompletionTimeAnalyticsQueryHandler(
            _readService,
            NullLogger<GetCompletionTimeAnalyticsQueryHandler>.Instance);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WithDataFromReadService()
    {
        // Arrange
        var expected = BuildAnalytics();
        _readService
            .GetCompletionTimesAsync(TenantId, From, To, null, Arg.Any<CancellationToken>())
            .Returns(expected);

        // Act
        var result = await _sut.Handle(
            new GetCompletionTimeAnalyticsQuery(TenantId, From, To), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task Handle_PassesAllParametersToReadService()
    {
        // Arrange
        var definitionId = Guid.NewGuid();
        _readService
            .GetCompletionTimesAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
                                     Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(BuildAnalytics());

        // Act
        await _sut.Handle(
            new GetCompletionTimeAnalyticsQuery(TenantId, From, To, definitionId),
            CancellationToken.None);

        // Assert
        await _readService.Received(1).GetCompletionTimesAsync(
            TenantId, From, To, definitionId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNoCompletedInstances_ReturnsEmptyDatasets()
    {
        // Arrange
        var empty = new CompletionTimeAnalyticsDto(
            ByDefinition: Array.Empty<WorkflowCompletionTimeDto>(),
            Bottlenecks:  Array.Empty<StepBottleneckDto>(),
            WeeklyTrend:  Array.Empty<CompletionTrendDto>());

        _readService
            .GetCompletionTimesAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
                                     Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(empty);

        // Act
        var result = await _sut.Handle(
            new GetCompletionTimeAnalyticsQuery(TenantId, From, To), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ByDefinition.Should().BeEmpty();
        result.Value.Bottlenecks.Should().BeEmpty();
        result.Value.WeeklyTrend.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithData_ReturnsAllThreeDatasets()
    {
        // Arrange
        var analytics = new CompletionTimeAnalyticsDto(
            ByDefinition: new List<WorkflowCompletionTimeDto>
            {
                new(Guid.NewGuid(), "Onboarding", AvgDurationMinutes: 120, InstanceCount: 5),
                new(Guid.NewGuid(), "Renewal",    AvgDurationMinutes:  90, InstanceCount: 3),
            },
            Bottlenecks: new List<StepBottleneckDto>
            {
                new("Document Review", AvgDurationMinutes: 75, OccurrenceCount: 8),
                new("Approval",        AvgDurationMinutes: 45, OccurrenceCount: 6),
            },
            WeeklyTrend: new List<CompletionTrendDto>
            {
                new(new DateOnly(2026, 1, 5), AvgDurationMinutes: 110, InstanceCount: 2),
                new(new DateOnly(2026, 1, 12), AvgDurationMinutes: 95, InstanceCount: 3),
            });

        _readService
            .GetCompletionTimesAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
                                     Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(analytics);

        // Act
        var result = await _sut.Handle(
            new GetCompletionTimeAnalyticsQuery(TenantId, From, To), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ByDefinition.Should().HaveCount(2);
        result.Value.ByDefinition[0].DefinitionName.Should().Be("Onboarding");
        result.Value.Bottlenecks.Should().HaveCount(2);
        result.Value.Bottlenecks[0].StepName.Should().Be("Document Review");
        result.Value.WeeklyTrend.Should().HaveCount(2);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static CompletionTimeAnalyticsDto BuildAnalytics() =>
        new(
            ByDefinition: Array.Empty<WorkflowCompletionTimeDto>(),
            Bottlenecks:  Array.Empty<StepBottleneckDto>(),
            WeeklyTrend:  Array.Empty<CompletionTrendDto>());
}
