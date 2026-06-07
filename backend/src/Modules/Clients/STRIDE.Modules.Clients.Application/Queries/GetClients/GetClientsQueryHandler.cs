using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Clients.Application.Abstractions;
using STRIDE.Modules.Clients.Application.DTOs;

namespace STRIDE.Modules.Clients.Application.Queries.GetClients;

internal sealed class GetClientsQueryHandler
    : IRequestHandler<GetClientsQuery, Result<PagedResult<ClientSummaryDto>>>
{
    private readonly IClientReadService _read;

    public GetClientsQueryHandler(IClientReadService read) => _read = read;

    public async Task<Result<PagedResult<ClientSummaryDto>>> Handle(
        GetClientsQuery request, CancellationToken cancellationToken)
    {
        var result = await _read.GetClientsAsync(
            request.TenantId, request.Search, request.Status,
            request.Page, request.PageSize, cancellationToken);

        return Result<PagedResult<ClientSummaryDto>>.Success(result);
    }
}
