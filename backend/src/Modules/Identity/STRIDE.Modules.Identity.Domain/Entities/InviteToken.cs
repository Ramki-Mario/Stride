using STRIDE.BuildingBlocks.Domain.Entities;

namespace STRIDE.Modules.Identity.Domain.Entities;

/// <summary>
/// Stores the SHA-256 hash of a single-use invite token issued when a user is invited
/// to STRIDE. The raw token travels by email only; only the hash is persisted.
/// </summary>
public sealed class InviteToken : AuditableEntity
{
    public Guid     UserId    { get; private set; }
    public string   TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public bool     IsUsed    { get; private set; }

    private InviteToken() { }

    public static InviteToken Create(
        Guid   tenantId,
        Guid   userId,
        string tokenHash,
        DateTime expiresAt,
        Guid   createdBy)
        => new()
        {
            Id        = Guid.NewGuid(),
            TenantId  = tenantId,
            UserId    = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            IsUsed    = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

    public bool IsValid(DateTime utcNow) => !IsDeleted && !IsUsed && ExpiresAt >= utcNow;

    public void MarkUsed()
    {
        IsUsed    = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
