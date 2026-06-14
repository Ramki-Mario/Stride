using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Scheduling.Application.Commands.DeactivateSchedule;

public sealed record DeactivateScheduleCommand(Guid Id, Guid DeactivatedBy) : IRequest<Result>;
