using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Scheduling.Application.Commands.DeleteSchedule;

public sealed record DeleteScheduleCommand(Guid Id) : IRequest<Result>;
