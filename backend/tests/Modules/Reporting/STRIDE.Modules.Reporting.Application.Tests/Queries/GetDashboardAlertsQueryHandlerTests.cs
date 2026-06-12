using Microsoft.Extensions.Logging.Abstractions;
using STRIDE.Modules.Reporting.Application.Abstractions;
using STRIDE.Modules.Reporting.Application.Queries.GetDashboardAlerts;
using STRIDE.Modules.Reporting.Application.ReadModels;

namespace STRIDE.Modules.Reporting.Application.Tests.Queries;

public sealed class GetDashboardAlertsQueryHandlerTests
{
    private readonly IReportingReadService _readService =
        Substitute.For<IReportingReadService>();

    private readonly GetDashboardAlertsQueryHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();

    public GetDashboardAlertsQueryHandlerTests()
    {
        _sut = new GetDashboardAlertsQueryHandler(
            _readService,
            NullLogger<GetDashboardAlertsQueryHandler>.Instance);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WithAlertSummaryFromReadService()
    {
        // Arrange
        var expected = BuildAlertSummary();
        _readService.GetDashboardAlertsAsync(TenantId, Arg.Any<CancellationToken>())
                    .Returns(expected);

        // Act
        var result = await _sut.Handle(new GetDashboardAlertsQuery(TenantId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task Handle_PassesTenantIdToReadService()
    {
        // Arrange
        _readService.GetDashboardAlertsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                    .Returns(BuildAlertSummary());

        // Act
        await _sut.Handle(new GetDashboardAlertsQuery(TenantId), CancellationToken.None);

        // Assert
        await _readService.Received(1)
            .GetDashboardAlertsAsync(TenantId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAllPanelsEmpty_ReturnsSuccessWithEmptyLists()
    {
        // Arrange
        var empty = new DashboardAlertSummaryDto(
            Overdue:         Array.Empty<OverdueAlertItemDto>(),
            UnassignedSteps: Array.Empty<UnassignedStepAlertItemDto>(),
            SlaAtRisk:       Array.Empty<SlaAtRiskAlertItemDto>(),
            ReadyToInvoice:  Array.Empty<ReadyToInvoiceAlertItemDto>());

        _readService.GetDashboardAlertsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                    .Returns(empty);

        // Act
        var result = await _sut.Handle(new GetDashboardAlertsQuery(TenantId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Overdue.Should().BeEmpty();
        result.Value.UnassignedSteps.Should().BeEmpty();
        result.Value.SlaAtRisk.Should().BeEmpty();
        result.Value.ReadyToInvoice.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenPanelsHaveItems_ReturnsAllItems()
    {
        // Arrange
        var summary = new DashboardAlertSummaryDto(
            Overdue: new List<OverdueAlertItemDto>
            {
                new(Guid.NewGuid(), "Job Alpha", "Client A", MinutesOverdue: 90),
            },
            UnassignedSteps: new List<UnassignedStepAlertItemDto>
            {
                new(Guid.NewGuid(), "Review", Guid.NewGuid(), "Job Beta", MinutesWaiting: 75),
            },
            SlaAtRisk: new List<SlaAtRiskAlertItemDto>
            {
                new(Guid.NewGuid(), "Job Gamma", null, MinutesRemaining: 120),
            },
            ReadyToInvoice: new List<ReadyToInvoiceAlertItemDto>
            {
                new(Guid.NewGuid(), "Job Delta", "Client D", DateTime.UtcNow.AddHours(-2)),
            });

        _readService.GetDashboardAlertsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                    .Returns(summary);

        // Act
        var result = await _sut.Handle(new GetDashboardAlertsQuery(TenantId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Overdue.Should().HaveCount(1);
        result.Value.Overdue[0].WorkflowName.Should().Be("Job Alpha");
        result.Value.UnassignedSteps.Should().HaveCount(1);
        result.Value.SlaAtRisk.Should().HaveCount(1);
        result.Value.ReadyToInvoice.Should().HaveCount(1);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static DashboardAlertSummaryDto BuildAlertSummary() =>
        new(
            Overdue:         Array.Empty<OverdueAlertItemDto>(),
            UnassignedSteps: Array.Empty<UnassignedStepAlertItemDto>(),
            SlaAtRisk:       Array.Empty<SlaAtRiskAlertItemDto>(),
            ReadyToInvoice:  Array.Empty<ReadyToInvoiceAlertItemDto>());
}
