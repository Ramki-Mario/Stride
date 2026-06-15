using MediatR;
using STRIDE.Modules.KitOps.Application.Abstractions;
using STRIDE.Modules.KitOps.Application.DTOs;

namespace STRIDE.Modules.KitOps.Application.Queries.GetKitCatalog;

internal sealed class GetKitCatalogQueryHandler
    : IRequestHandler<GetKitCatalogQuery, IReadOnlyList<KitCatalogItemDto>>
{
    private readonly IKitUserHistoryReadService _history;

    public GetKitCatalogQueryHandler(IKitUserHistoryReadService history) => _history = history;

    public Task<IReadOnlyList<KitCatalogItemDto>> Handle(
        GetKitCatalogQuery request, CancellationToken cancellationToken)
        => _history.GetCatalogWithAvailabilityAsync(request.TenantId, cancellationToken);
}
