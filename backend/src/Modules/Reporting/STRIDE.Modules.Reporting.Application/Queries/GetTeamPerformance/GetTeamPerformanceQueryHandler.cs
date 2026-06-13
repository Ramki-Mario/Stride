using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Reporting.Application.Abstractions;
using STRIDE.Modules.Reporting.Application.ReadModels;

namespace STRIDE.Modules.Reporting.Application.Queries.GetTeamPerformance;

internal sealed class GetTeamPerformanceQueryHandler
    : IRequestHandler<GetTeamPerformanceQuery, Result<IReadOnlyList<TeamMemberPerformanceDto>>>
{
    private readonly IAnalyticsReadService                      _readService;
    private readonly ILogger<GetTeamPerformanceQueryHandler>    _logger;

    public GetTeamPerformanceQueryHandler(
        IAnalyticsReadService readService,
        ILogger<GetTeamPerformanceQueryHandler> logger)
    {
        _readService = readService;
        _logger      = logger;
    }

    public async Task<Result<IReadOnlyList<TeamMemberPerformanceDto>>> Handle(
        GetTeamPerformanceQuery request,
        CancellationToken cancellationToken)
    {
        _logger.GetTeamPerformance(request.TenantId, request.FromDate, request.ToDate);

        var data = await _readService.GetTeamPerformanceAsync(
            request.TenantId,
            request.FromDate,
            request.ToDate,
            request.RoleId,
            cancellationToken);

        return Result.Success(data);
    }
}
