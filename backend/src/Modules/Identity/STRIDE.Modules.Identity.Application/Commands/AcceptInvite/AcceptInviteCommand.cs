using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Identity.Application.Commands.LoginUser;

namespace STRIDE.Modules.Identity.Application.Commands.AcceptInvite;

public sealed record AcceptInviteCommand(
    string Token,
    string Password,
    string ConfirmPassword) : IRequest<Result<LoginResult>>;
