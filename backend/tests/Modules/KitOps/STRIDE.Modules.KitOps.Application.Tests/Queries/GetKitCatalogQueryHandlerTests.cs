using STRIDE.Modules.KitOps.Application.Abstractions;
using STRIDE.Modules.KitOps.Application.DTOs;
using STRIDE.Modules.KitOps.Application.Queries.GetKitCatalog;

namespace STRIDE.Modules.KitOps.Application.Tests.Queries;

public sealed class GetKitCatalogQueryHandlerTests
{
    private readonly IKitUserHistoryReadService _history = Substitute.For<IKitUserHistoryReadService>();
    private readonly GetKitCatalogQueryHandler  _handler;

    private static readonly Guid TenantId = Guid.NewGuid();

    public GetKitCatalogQueryHandlerTests()
    {
        _handler = new GetKitCatalogQueryHandler(_history);
    }

    [Fact]
    public async Task Handle_DelegatesToReadService_WithCorrectTenant()
    {
        var expected = new List<KitCatalogItemDto>
        {
            new(Guid.NewGuid(), "Radio HT1000", "Communications", null, 5, 2, 3, 0),
            new(Guid.NewGuid(), "Body Camera",  "Safety",         null, 3, 3, 0, 1),
        };
        _history.GetCatalogWithAvailabilityAsync(TenantId, Arg.Any<CancellationToken>())
                .Returns((IReadOnlyList<KitCatalogItemDto>)expected);

        var result = await _handler.Handle(
            new GetKitCatalogQuery(TenantId), CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
        await _history.Received(1).GetCatalogWithAvailabilityAsync(TenantId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EmptyCatalog_ReturnsEmptyList()
    {
        _history.GetCatalogWithAvailabilityAsync(TenantId, Arg.Any<CancellationToken>())
                .Returns((IReadOnlyList<KitCatalogItemDto>)new List<KitCatalogItemDto>());

        var result = await _handler.Handle(
            new GetKitCatalogQuery(TenantId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenThrough()
    {
        using var cts = new CancellationTokenSource();
        _history.GetCatalogWithAvailabilityAsync(Arg.Any<Guid>(), cts.Token)
                .Returns((IReadOnlyList<KitCatalogItemDto>)new List<KitCatalogItemDto>());

        await _handler.Handle(new GetKitCatalogQuery(TenantId), cts.Token);

        await _history.Received(1).GetCatalogWithAvailabilityAsync(TenantId, cts.Token);
    }
}
