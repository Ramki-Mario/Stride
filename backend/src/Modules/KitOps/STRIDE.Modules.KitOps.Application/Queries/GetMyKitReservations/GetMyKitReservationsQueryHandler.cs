using MediatR;
using STRIDE.Modules.KitOps.Application.Abstractions;
using STRIDE.Modules.KitOps.Application.DTOs;

namespace STRIDE.Modules.KitOps.Application.Queries.GetMyKitReservations;

internal sealed class GetMyKitReservationsQueryHandler
    : IRequestHandler<GetMyKitReservationsQuery, IReadOnlyList<MyKitReservationDto>>
{
    private readonly IKitUserHistoryReadService _history;

    public GetMyKitReservationsQueryHandler(IKitUserHistoryReadService history) => _history = history;

    public Task<IReadOnlyList<MyKitReservationDto>> Handle(
        GetMyKitReservationsQuery request, CancellationToken cancellationToken)
        => _history.GetMyReservationsAsync(request.TenantId, request.UserId, cancellationToken);
}
