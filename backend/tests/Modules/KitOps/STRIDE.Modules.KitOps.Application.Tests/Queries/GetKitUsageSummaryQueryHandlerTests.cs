using STRIDE.Modules.KitOps.Application.Abstractions;
using STRIDE.Modules.KitOps.Application.DTOs;
using STRIDE.Modules.KitOps.Application.Queries.GetKitUsageSummary;

namespace STRIDE.Modules.KitOps.Application.Tests.Queries;

public sealed class GetKitUsageSummaryQueryHandlerTests
{
    private readonly IKitUsageReportService _reports = Substitute.For<IKitUsageReportService>();
    private readonly GetKitUsageSummaryQueryHandler _handler;

    private static readonly Guid TenantId = Guid.NewGuid();

    public GetKitUsageSummaryQueryHandlerTests()
        => _handler = new GetKitUsageSummaryQueryHandler(_reports);

    private static KitUsageSummaryRowDto ARow() => new(
        KitItemId:           Guid.NewGuid(),
        Name:                "Radio HT1000",
        Category:            "Communications",
        TotalQuantity:       5,
        TotalCheckouts:      12,
        ActiveCheckouts:     2,
        OverdueCheckouts:    0,
        PendingReservations: 1,
        AvgDaysCheckedOut:   3.5);

    [Fact]
    public async Task Handle_NoFilters_DelegatesToReportService()
    {
        var expected = new List<KitUsageSummaryRowDto> { ARow() };
        _reports.GetUsageSummaryAsync(TenantId, null, null, Arg.Any<CancellationToken>())
                .Returns(expected);

        var result = await _handler.Handle(
            new GetKitUsageSummaryQuery(TenantId, null, null),
            CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Handle_WithDateRange_PassesFiltersThrough()
    {
        var from = DateTime.UtcNow.AddDays(-30);
        var to   = DateTime.UtcNow;
        _reports.GetUsageSummaryAsync(TenantId, from, to, Arg.Any<CancellationToken>())
                .Returns(new List<KitUsageSummaryRowDto>());

        await _handler.Handle(
            new GetKitUsageSummaryQuery(TenantId, from, to),
            CancellationToken.None);

        await _reports.Received(1).GetUsageSummaryAsync(
            TenantId, from, to, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyResult_ReturnsEmptyList()
    {
        _reports.GetUsageSummaryAsync(TenantId, null, null, Arg.Any<CancellationToken>())
                .Returns(new List<KitUsageSummaryRowDto>());

        var result = await _handler.Handle(
            new GetKitUsageSummaryQuery(TenantId, null, null),
            CancellationToken.None);

        result.Should().BeEmpty();
    }
}
