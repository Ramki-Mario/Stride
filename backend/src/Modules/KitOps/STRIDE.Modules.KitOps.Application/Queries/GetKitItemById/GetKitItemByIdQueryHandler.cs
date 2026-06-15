using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.KitOps.Application.Abstractions;
using STRIDE.Modules.KitOps.Application.DTOs;

namespace STRIDE.Modules.KitOps.Application.Queries.GetKitItemById;

internal sealed class GetKitItemByIdQueryHandler
    : IRequestHandler<GetKitItemByIdQuery, Result<KitItemDetailDto>>
{
    private readonly IKitItemReadService _read;

    public GetKitItemByIdQueryHandler(IKitItemReadService read) => _read = read;

    public async Task<Result<KitItemDetailDto>> Handle(
        GetKitItemByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await _read.GetKitItemByIdAsync(request.TenantId, request.KitItemId, cancellationToken);

        return dto is null
            ? Result.Failure<KitItemDetailDto>($"Kit item '{request.KitItemId}' not found.")
            : Result.Success(dto);
    }
}
