using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Scheduling.Application.DTOs;

namespace STRIDE.Modules.Scheduling.Application.Queries.GetScheduleDefinition;

public sealed record GetScheduleDefinitionQuery(Guid Id) : IRequest<Result<ScheduleDefinitionDto>>;
