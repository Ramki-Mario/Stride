using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Scheduling.Application.Commands.ActivateSchedule;

public sealed record ActivateScheduleCommand(Guid Id, Guid ActivatedBy) : IRequest<Result>;
