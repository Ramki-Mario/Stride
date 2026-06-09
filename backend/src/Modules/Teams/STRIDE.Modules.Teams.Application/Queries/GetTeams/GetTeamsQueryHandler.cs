using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Teams.Application.Abstractions;
using STRIDE.Modules.Teams.Application.DTOs;

namespace STRIDE.Modules.Teams.Application.Queries.GetTeams;

internal sealed class GetTeamsQueryHandler
    : IRequestHandler<GetTeamsQuery, Result<PagedResult<TeamSummaryDto>>>
{
    private readonly ITeamReadService _read;

    public GetTeamsQueryHandler(ITeamReadService read) => _read = read;

    public async Task<Result<PagedResult<TeamSummaryDto>>> Handle(
        GetTeamsQuery request, CancellationToken cancellationToken)
    {
        var result = await _read.GetTeamsAsync(
            request.TenantId, request.Search, request.Status,
            request.Page, request.PageSize, cancellationToken);

        return Result<PagedResult<TeamSummaryDto>>.Success(result);
    }
}
