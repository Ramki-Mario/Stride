namespace STRIDE.Modules.Workflows.Application.Commands.CreateSharedLink;

/// <summary>
/// Management-side view of a shared link (returned to the authenticated workflow owner).
/// The public client-facing view is a separate, redacted DTO (US-178).
/// </summary>
public sealed record SharedLinkDto(
    Guid      Id,
    string    Token,
    string    ShareUrl,
    DateTime  ExpiresAt,
    DateTime? RevokedAt,
    int       ViewCount,
    bool      IsExpired,
    bool      IsActive,
    DateTime  CreatedAt);
