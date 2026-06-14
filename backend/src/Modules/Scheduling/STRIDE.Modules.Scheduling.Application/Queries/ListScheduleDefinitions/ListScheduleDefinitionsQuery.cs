using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Scheduling.Application.DTOs;

namespace STRIDE.Modules.Scheduling.Application.Queries.ListScheduleDefinitions;

public sealed record ListScheduleDefinitionsQuery : IRequest<Result<IReadOnlyList<ScheduleDefinitionSummaryDto>>>;
