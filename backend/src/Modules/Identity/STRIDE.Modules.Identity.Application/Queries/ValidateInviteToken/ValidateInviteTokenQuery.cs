using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Identity.Application.Queries.ValidateInviteToken;

public sealed record ValidateInviteTokenQuery(string Token)
    : IRequest<Result<InviteTokenInfoDto>>;
