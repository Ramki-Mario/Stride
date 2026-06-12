using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Reporting.Application.ReadModels;

namespace STRIDE.Modules.Reporting.Application.Queries.GetTeamWorkload;

public sealed record GetTeamWorkloadQuery(Guid TenantId)
    : IRequest<Result<IReadOnlyList<TeamWorkloadItemDto>>>;
