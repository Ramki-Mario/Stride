using Microsoft.Extensions.Logging.Abstractions;
using STRIDE.Modules.Reporting.Application.Abstractions;
using STRIDE.Modules.Reporting.Application.Queries.GetRevenueAnalytics;
using STRIDE.Modules.Reporting.Application.ReadModels;

namespace STRIDE.Modules.Reporting.Application.Tests.Queries;

public sealed class GetRevenueAnalyticsQueryHandlerTests
{
    private readonly IAnalyticsReadService _readService =
        Substitute.For<IAnalyticsReadService>();

    private readonly GetRevenueAnalyticsQueryHandler _sut;

    private static readonly Guid     TenantId = Guid.NewGuid();
    private static readonly DateTime From     = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime To       = new(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc);

    public GetRevenueAnalyticsQueryHandlerTests()
    {
        _sut = new GetRevenueAnalyticsQueryHandler(
            _readService,
            NullLogger<GetRevenueAnalyticsQueryHandler>.Instance);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WithDataFromReadService()
    {
        // Arrange
        SetupEmptyRevenue();

        // Act
        var result = await _sut.Handle(
            new GetRevenueAnalyticsQuery(TenantId, From, To), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_PassesAllParametersToReadService()
    {
        // Arrange
        SetupEmptyRevenue();

        // Act
        await _sut.Handle(
            new GetRevenueAnalyticsQuery(TenantId, From, To), CancellationToken.None);

        // Assert
        await _readService.Received(1).GetRevenueAsync(
            TenantId, From, To, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNoRevenue_ReturnsSummaryWithZeroes()
    {
        // Arrange
        SetupEmptyRevenue();

        // Act
        var result = await _sut.Handle(
            new GetRevenueAnalyticsQuery(TenantId, From, To), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Summary.TotalInvoiced.Should().Be(0m);
        result.Value.Summary.TotalPaid.Should().Be(0m);
        result.Value.Summary.Outstanding.Should().Be(0m);
        result.Value.MonthlyTrend.Should().BeEmpty();
        result.Value.ByWorkflowType.Should().BeEmpty();
        result.Value.ByClient.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithRevenue_ComputesSummaryFromMonthlyData()
    {
        // Arrange
        var monthly = new List<RevenueMonthlyDto>
        {
            new(new DateOnly(2026, 1, 1), InvoicedAmount: 5000m, PaidAmount: 5000m),
            new(new DateOnly(2026, 2, 1), InvoicedAmount: 3000m, PaidAmount: 0m),
        };

        _readService
            .GetRevenueAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
                             Arg.Any<CancellationToken>())
            .Returns((
                (IReadOnlyList<RevenueMonthlyDto>)monthly,
                (IReadOnlyList<RevenueByWorkflowTypeDto>)Array.Empty<RevenueByWorkflowTypeDto>(),
                (IReadOnlyList<RevenueByClientDto>)Array.Empty<RevenueByClientDto>()));

        // Act
        var result = await _sut.Handle(
            new GetRevenueAnalyticsQuery(TenantId, From, To), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Summary.TotalInvoiced.Should().Be(8000m);
        result.Value.Summary.TotalPaid.Should().Be(5000m);
        result.Value.Summary.Outstanding.Should().Be(3000m);
        result.Value.MonthlyTrend.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithAllDatasets_ReturnsAllThreeBreakdowns()
    {
        // Arrange
        var monthly = new List<RevenueMonthlyDto>
        {
            new(new DateOnly(2026, 1, 1), InvoicedAmount: 10000m, PaidAmount: 7500m),
        };

        var byWorkflow = new List<RevenueByWorkflowTypeDto>
        {
            new("Onboarding", TotalAmount: 6000m, InvoiceCount: 3),
            new("Renewal",    TotalAmount: 4000m, InvoiceCount: 2),
        };

        var byClient = new List<RevenueByClientDto>
        {
            new("Acme Corp", TotalAmount: 7000m, PaidAmount: 5000m, InvoiceCount: 2),
            new("Beta Ltd",  TotalAmount: 3000m, PaidAmount: 2500m, InvoiceCount: 1),
        };

        _readService
            .GetRevenueAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
                             Arg.Any<CancellationToken>())
            .Returns((
                (IReadOnlyList<RevenueMonthlyDto>)monthly,
                (IReadOnlyList<RevenueByWorkflowTypeDto>)byWorkflow,
                (IReadOnlyList<RevenueByClientDto>)byClient));

        // Act
        var result = await _sut.Handle(
            new GetRevenueAnalyticsQuery(TenantId, From, To), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ByWorkflowType.Should().HaveCount(2);
        result.Value.ByWorkflowType[0].WorkflowType.Should().Be("Onboarding");
        result.Value.ByClient.Should().HaveCount(2);
        result.Value.ByClient[0].ClientName.Should().Be("Acme Corp");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void SetupEmptyRevenue()
    {
        _readService
            .GetRevenueAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
                             Arg.Any<CancellationToken>())
            .Returns((
                (IReadOnlyList<RevenueMonthlyDto>)Array.Empty<RevenueMonthlyDto>(),
                (IReadOnlyList<RevenueByWorkflowTypeDto>)Array.Empty<RevenueByWorkflowTypeDto>(),
                (IReadOnlyList<RevenueByClientDto>)Array.Empty<RevenueByClientDto>()));
    }
}
