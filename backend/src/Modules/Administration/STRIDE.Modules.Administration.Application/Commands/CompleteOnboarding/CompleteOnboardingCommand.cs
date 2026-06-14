using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Administration.Application.Commands.CompleteOnboarding;

public sealed record CompleteOnboardingCommand(Guid TenantId, Guid ActorId) : IRequest<Result>;
