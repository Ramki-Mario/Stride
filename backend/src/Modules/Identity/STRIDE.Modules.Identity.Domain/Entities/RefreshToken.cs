using STRIDE.BuildingBlocks.Domain.Entities;

namespace STRIDE.Modules.Identity.Domain.Entities;

public sealed class RefreshToken : AuditableEntity
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    /// <summary>Token that replaced this one during rotation; null when not yet rotated.</summary>
    public string? ReplacedByToken { get; private set; }

    public bool IsActive(DateTime utcNow)
        => !IsDeleted && RevokedAt is null && utcNow < ExpiresAt;

    private RefreshToken() { }

    public static RefreshToken Create(
        Guid tenantId,
        Guid userId,
        string token,
        DateTime expiresAt,
        Guid createdBy)
    {
        var now = DateTime.UtcNow;
        return new RefreshToken
        {
            Id              = Guid.NewGuid(),
            TenantId        = tenantId,
            UserId          = userId,
            Token           = token,
            ExpiresAt       = expiresAt,
            CreatedAt       = now,
            UpdatedAt       = now,
            CreatedBy       = createdBy,
            IsDeleted       = false,
        };
    }

    public void Revoke(DateTime utcNow, string? replacedByToken = null)
    {
        RevokedAt        = utcNow;
        ReplacedByToken  = replacedByToken;
        UpdatedAt        = utcNow;
    }
}
