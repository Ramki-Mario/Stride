using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Teams.Application.Abstractions;
using STRIDE.Modules.Teams.Application.DTOs;

namespace STRIDE.Modules.Teams.Application.Queries.GetTeamById;

internal sealed class GetTeamByIdQueryHandler
    : IRequestHandler<GetTeamByIdQuery, Result<TeamDetailDto>>
{
    private readonly ITeamReadService _read;

    public GetTeamByIdQueryHandler(ITeamReadService read) => _read = read;

    public async Task<Result<TeamDetailDto>> Handle(
        GetTeamByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await _read.GetTeamByIdAsync(request.TenantId, request.TeamId, cancellationToken);

        return dto is null
            ? Result.Failure<TeamDetailDto>($"Team '{request.TeamId}' not found.")
            : Result<TeamDetailDto>.Success(dto);
    }
}
