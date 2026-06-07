using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Clients.Application.Abstractions;
using STRIDE.Modules.Clients.Application.DTOs;

namespace STRIDE.Modules.Clients.Application.Queries.GetClientById;

internal sealed class GetClientByIdQueryHandler
    : IRequestHandler<GetClientByIdQuery, Result<ClientDetailDto>>
{
    private readonly IClientReadService _read;

    public GetClientByIdQueryHandler(IClientReadService read) => _read = read;

    public async Task<Result<ClientDetailDto>> Handle(
        GetClientByIdQuery request, CancellationToken cancellationToken)
    {
        var client = await _read.GetClientByIdAsync(request.TenantId, request.ClientId, cancellationToken);

        return client is null
            ? Result<ClientDetailDto>.Failure("Client not found.")
            : Result<ClientDetailDto>.Success(client);
    }
}
