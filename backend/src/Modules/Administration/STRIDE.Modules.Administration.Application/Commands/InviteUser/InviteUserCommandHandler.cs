using System.Security.Cryptography;
using System.Text;
using MediatR;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Administration.Application.Abstractions;

namespace STRIDE.Modules.Administration.Application.Commands.InviteUser;

internal sealed class InviteUserCommandHandler
    : IRequestHandler<InviteUserCommand, Result<Guid>>
{
    private const int TokenExpiryHours = 48;

    private readonly IAdminWriteService _writeService;
    private readonly IEmailSender       _emailSender;
    private readonly IAuditLogger       _audit;
    private readonly ICurrentUser       _currentUser;

    public InviteUserCommandHandler(
        IAdminWriteService writeService,
        IEmailSender       emailSender,
        IAuditLogger       audit,
        ICurrentUser       currentUser)
    {
        _writeService = writeService;
        _emailSender  = emailSender;
        _audit        = audit;
        _currentUser  = currentUser;
    }

    public async Task<Result<Guid>> Handle(InviteUserCommand request, CancellationToken cancellationToken)
    {
        var userId = await _writeService.InviteUserAsync(
            request.TenantId,
            request.Email,
            request.DisplayName,
            request.Role,
            request.InvitedBy,
            cancellationToken);

        // Generate a 256-bit secure random token; only its SHA-256 hash is stored.
        var rawToken  = GenerateRawToken();
        var tokenHash = HashToken(rawToken);
        var expiresAt = DateTime.UtcNow.AddHours(TokenExpiryHours);

        await _writeService.CreateInviteTokenAsync(
            request.TenantId, userId, tokenHash, expiresAt, request.InvitedBy, cancellationToken);

        var acceptUrl = $"/accept-invite?token={Uri.EscapeDataString(rawToken)}";
        await _emailSender.SendAsync(
            to:          request.Email,
            subject:     "You have been invited to STRIDE",
            htmlBody:    BuildEmailBody(request.DisplayName, acceptUrl),
            cancellationToken: cancellationToken);

        await _audit.LogAsync(new AuditLogEntry(
            TenantId:     request.TenantId,
            ActorId:      request.InvitedBy,
            ActorEmail:   _currentUser.Email,
            Action:       AuditActions.UserInvited,
            ResourceType: "User",
            ResourceId:   userId,
            NewValueJson: $"{{\"email\":\"{request.Email}\",\"role\":\"{request.Role}\"}}"));

        return Result.Success(userId);
    }

    private static string GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32); // 256 bits
        return Convert.ToBase64String(bytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('='); // base64url
    }

    private static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string BuildEmailBody(string displayName, string acceptUrl) =>
        $"""
        <html><body style="font-family:sans-serif;color:#111;max-width:600px;margin:40px auto">
          <h2 style="color:#B97AF9">You've been invited to STRIDE</h2>
          <p>Hi {System.Net.WebUtility.HtmlEncode(displayName)},</p>
          <p>An admin has invited you to join their STRIDE workspace.
             Click the button below to set your password and activate your account.</p>
          <p style="margin:32px 0">
            <a href="{acceptUrl}"
               style="background:#B97AF9;color:#fff;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:600">
              Accept Invitation
            </a>
          </p>
          <p style="color:#888;font-size:13px">This link expires in 48 hours. If you were not expecting this email, you can safely ignore it.</p>
        </body></html>
        """;
}
