using System.Security.Cryptography;
using System.Text;
using MediatR;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Identity.Application.Abstractions;

namespace STRIDE.Modules.Identity.Application.Queries.ValidateInviteToken;

internal sealed class ValidateInviteTokenQueryHandler
    : IRequestHandler<ValidateInviteTokenQuery, Result<InviteTokenInfoDto>>
{
    private readonly IInviteTokenRepository _inviteTokens;
    private readonly IUserRepository        _users;
    private readonly ITenantContextSetter   _tenantSetter;

    public ValidateInviteTokenQueryHandler(
        IInviteTokenRepository inviteTokens,
        IUserRepository        users,
        ITenantContextSetter   tenantSetter)
    {
        _inviteTokens = inviteTokens;
        _users        = users;
        _tenantSetter = tenantSetter;
    }

    public async Task<Result<InviteTokenInfoDto>> Handle(
        ValidateInviteTokenQuery query,
        CancellationToken        cancellationToken)
    {
        var tokenHash = HashToken(query.Token);
        var invite    = await _inviteTokens.GetByHashAsync(tokenHash, cancellationToken);

        if (invite is null || !invite.IsValid(DateTime.UtcNow))
            return Result.Failure<InviteTokenInfoDto>("The invite link is invalid or has expired.");

        _tenantSetter.SetTenantId(invite.TenantId);

        var user = await _users.GetByIdAsync(invite.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<InviteTokenInfoDto>("User not found.");

        return Result.Success(new InviteTokenInfoDto(user.Email, user.DisplayName));
    }

    private static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
