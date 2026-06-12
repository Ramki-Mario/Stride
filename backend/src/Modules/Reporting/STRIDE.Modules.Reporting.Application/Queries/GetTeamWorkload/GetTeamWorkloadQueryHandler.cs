using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Reporting.Application.Abstractions;
using STRIDE.Modules.Reporting.Application.ReadModels;

namespace STRIDE.Modules.Reporting.Application.Queries.GetTeamWorkload;

internal sealed class GetTeamWorkloadQueryHandler
    : IRequestHandler<GetTeamWorkloadQuery, Result<IReadOnlyList<TeamWorkloadItemDto>>>
{
    private readonly IReportingReadService              _readService;
    private readonly ILogger<GetTeamWorkloadQueryHandler> _logger;

    public GetTeamWorkloadQueryHandler(
        IReportingReadService readService,
        ILogger<GetTeamWorkloadQueryHandler> logger)
    {
        _readService = readService;
        _logger      = logger;
    }

    public async Task<Result<IReadOnlyList<TeamWorkloadItemDto>>> Handle(
        GetTeamWorkloadQuery request,
        CancellationToken cancellationToken)
    {
        _logger.GetTeamWorkload(request.TenantId);
        var workload = await _readService.GetTeamWorkloadAsync(request.TenantId, cancellationToken);
        return Result.Success(workload);
    }
}
