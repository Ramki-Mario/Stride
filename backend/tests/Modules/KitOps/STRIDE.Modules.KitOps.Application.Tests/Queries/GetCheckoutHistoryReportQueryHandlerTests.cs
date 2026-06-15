using STRIDE.Modules.KitOps.Application.Abstractions;
using STRIDE.Modules.KitOps.Application.DTOs;
using STRIDE.Modules.KitOps.Application.Queries.GetCheckoutHistoryReport;

namespace STRIDE.Modules.KitOps.Application.Tests.Queries;

public sealed class GetCheckoutHistoryReportQueryHandlerTests
{
    private readonly IKitUsageReportService _reports = Substitute.For<IKitUsageReportService>();
    private readonly GetCheckoutHistoryReportQueryHandler _handler;

    private static readonly Guid TenantId = Guid.NewGuid();

    public GetCheckoutHistoryReportQueryHandlerTests()
        => _handler = new GetCheckoutHistoryReportQueryHandler(_reports);

    private static KitCheckoutReportRowDto ARow() => new(
        CheckoutId:         Guid.NewGuid(),
        KitItemName:        "Radio HT1000",
        Category:           "Communications",
        CheckedOutByUserId: Guid.NewGuid(),
        CheckedOutByEmail:  "field@test.com",
        CheckedOutAt:       DateTime.UtcNow.AddDays(-3),
        ExpectedReturnAt:   DateTime.UtcNow.AddDays(1),
        ReturnedAt:         null,
        Status:             "Active",
        DaysCheckedOut:     3);

    [Fact]
    public async Task Handle_NoFilters_DelegatesToReportService()
    {
        var expected = new List<KitCheckoutReportRowDto> { ARow() };
        _reports.GetCheckoutHistoryAsync(TenantId, null, null, null, Arg.Any<CancellationToken>())
                .Returns(expected);

        var result = await _handler.Handle(
            new GetCheckoutHistoryReportQuery(TenantId, null, null, null),
            CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Handle_WithDateRange_PassesFiltersThrough()
    {
        var from = DateTime.UtcNow.AddDays(-30);
        var to   = DateTime.UtcNow;
        _reports.GetCheckoutHistoryAsync(TenantId, from, to, null, Arg.Any<CancellationToken>())
                .Returns(new List<KitCheckoutReportRowDto>());

        await _handler.Handle(
            new GetCheckoutHistoryReportQuery(TenantId, from, to, null),
            CancellationToken.None);

        await _reports.Received(1).GetCheckoutHistoryAsync(
            TenantId, from, to, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithKitItemFilter_PassesKitItemIdThrough()
    {
        var kitItemId = Guid.NewGuid();
        _reports.GetCheckoutHistoryAsync(TenantId, null, null, kitItemId, Arg.Any<CancellationToken>())
                .Returns(new List<KitCheckoutReportRowDto>());

        await _handler.Handle(
            new GetCheckoutHistoryReportQuery(TenantId, null, null, kitItemId),
            CancellationToken.None);

        await _reports.Received(1).GetCheckoutHistoryAsync(
            TenantId, null, null, kitItemId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyResult_ReturnsEmptyList()
    {
        _reports.GetCheckoutHistoryAsync(TenantId, null, null, null, Arg.Any<CancellationToken>())
                .Returns(new List<KitCheckoutReportRowDto>());

        var result = await _handler.Handle(
            new GetCheckoutHistoryReportQuery(TenantId, null, null, null),
            CancellationToken.None);

        result.Should().BeEmpty();
    }
}
