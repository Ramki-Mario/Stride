using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Identity.Application.Abstractions;
using STRIDE.Modules.Identity.Application.Commands.LoginUser;

namespace STRIDE.Modules.Identity.Application.Commands.AcceptInvite;

internal sealed class AcceptInviteCommandHandler
    : IRequestHandler<AcceptInviteCommand, Result<LoginResult>>
{
    private readonly IInviteTokenRepository              _inviteTokens;
    private readonly IUserRepository                     _users;
    private readonly IPasswordHasher                     _hasher;
    private readonly ILoginResultBuilder                 _loginResultBuilder;
    private readonly ITenantContextSetter                _tenantSetter;
    private readonly ILogger<AcceptInviteCommandHandler> _logger;

    public AcceptInviteCommandHandler(
        IInviteTokenRepository              inviteTokens,
        IUserRepository                     users,
        IPasswordHasher                     hasher,
        ILoginResultBuilder                 loginResultBuilder,
        ITenantContextSetter                tenantSetter,
        ILogger<AcceptInviteCommandHandler> logger)
    {
        _inviteTokens       = inviteTokens;
        _users              = users;
        _hasher             = hasher;
        _loginResultBuilder = loginResultBuilder;
        _tenantSetter       = tenantSetter;
        _logger             = logger;
    }

    public async Task<Result<LoginResult>> Handle(
        AcceptInviteCommand request,
        CancellationToken   cancellationToken)
    {
        var tokenHash = HashToken(request.Token);
        var invite    = await _inviteTokens.GetByHashAsync(tokenHash, cancellationToken);

        if (invite is null || !invite.IsValid(DateTime.UtcNow))
        {
            _logger.LogWarning("Accept-invite attempt with invalid/expired token hash {Hash}.", tokenHash[..8]);
            return Result.Failure<LoginResult>("The invite link is invalid or has expired.");
        }

        _tenantSetter.SetTenantId(invite.TenantId);

        var user = await _users.GetByIdAsync(invite.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<LoginResult>("User not found.");

        var passwordHash = _hasher.Hash(request.Password);
        user.UpdatePassword(passwordHash);
        user.Reactivate();

        invite.MarkUsed();
        await _inviteTokens.SaveChangesAsync(cancellationToken);

        var loginResult = await _loginResultBuilder.BuildAsync(user, invite.TenantId, cancellationToken);

        _logger.LogInformation("User {UserId} accepted invite and activated account.", user.Id);

        return Result.Success(loginResult);
    }

    private static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
