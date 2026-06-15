using MediatR;
using STRIDE.Modules.KitOps.Application.Abstractions;
using STRIDE.Modules.KitOps.Application.DTOs;

namespace STRIDE.Modules.KitOps.Application.Queries.GetMyKitCheckouts;

internal sealed class GetMyKitCheckoutsQueryHandler
    : IRequestHandler<GetMyKitCheckoutsQuery, IReadOnlyList<MyKitCheckoutDto>>
{
    private readonly IKitUserHistoryReadService _history;

    public GetMyKitCheckoutsQueryHandler(IKitUserHistoryReadService history) => _history = history;

    public Task<IReadOnlyList<MyKitCheckoutDto>> Handle(
        GetMyKitCheckoutsQuery request, CancellationToken cancellationToken)
        => _history.GetMyCheckoutsAsync(request.TenantId, request.UserId, cancellationToken);
}
