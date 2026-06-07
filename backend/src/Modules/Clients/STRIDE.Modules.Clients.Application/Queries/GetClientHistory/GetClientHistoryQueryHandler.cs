using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Clients.Application.Abstractions;
using STRIDE.Modules.Clients.Application.DTOs;

namespace STRIDE.Modules.Clients.Application.Queries.GetClientHistory;

internal sealed class GetClientHistoryQueryHandler
    : IRequestHandler<GetClientHistoryQuery, Result<ClientHistoryDto>>
{
    private readonly IClientReadService _reader;

    public GetClientHistoryQueryHandler(IClientReadService reader) => _reader = reader;

    public async Task<Result<ClientHistoryDto>> Handle(
        GetClientHistoryQuery request, CancellationToken cancellationToken)
    {
        var history = await _reader.GetClientHistoryAsync(
            request.TenantId, request.ClientId, cancellationToken);

        return history is null
            ? Result<ClientHistoryDto>.Failure("Client not found.")
            : Result<ClientHistoryDto>.Success(history);
    }
}
