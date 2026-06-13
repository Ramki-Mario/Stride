using System.Security.Cryptography;
using STRIDE.BuildingBlocks.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Domain.Entities;

/// <summary>
/// A secure, time-limited link that lets an external client view a workflow instance
/// without a STRIDE account (EP-058). The <see cref="Token"/> is the only credential,
/// so it is a 256-bit cryptographically-random value (not a sequential or guessable id).
///
/// Lifecycle: created → (viewed n times) → expired (after <see cref="ExpiresAt"/>) or
/// revoked (<see cref="RevokedAt"/> set). Multiple links may be active for one instance.
/// </summary>
public sealed class SharedWorkflowLink : AuditableEntity
{
    /// <summary>Default lifetime of a freshly minted link.</summary>
    public const int DefaultExpiryDays = 30;

    private const int TokenByteLength = 32; // 256 bits

    public Guid     WorkflowInstanceId { get; private set; }

    /// <summary>URL-safe, cryptographically-random secret. This is the link's only credential.</summary>
    public string   Token              { get; private set; } = string.Empty;

    public DateTime ExpiresAt          { get; private set; }

    /// <summary>Non-null once the link has been revoked; revocation is immediate and permanent.</summary>
    public DateTime? RevokedAt         { get; private set; }

    /// <summary>Number of times the public endpoint has resolved this token (US-178).</summary>
    public int      ViewCount          { get; private set; }

    private SharedWorkflowLink() { }

    /// <summary>
    /// Mints a new shareable link for the given instance.
    /// </summary>
    /// <param name="expiryDays">Lifetime in days; defaults to <see cref="DefaultExpiryDays"/>.</param>
    public static SharedWorkflowLink Create(
        Guid workflowInstanceId,
        Guid tenantId,
        Guid createdByUserId,
        int  expiryDays = DefaultExpiryDays)
    {
        if (workflowInstanceId == Guid.Empty)
            throw new WorkflowDomainException("WorkflowInstanceId is required to create a shared link.");

        if (expiryDays <= 0)
            throw new WorkflowDomainException("Shared link expiry must be a positive number of days.");

        var now = DateTime.UtcNow;
        return new SharedWorkflowLink
        {
            Id                 = Guid.NewGuid(),
            WorkflowInstanceId = workflowInstanceId,
            TenantId           = tenantId,
            Token              = GenerateToken(),
            ExpiresAt          = now.AddDays(expiryDays),
            RevokedAt          = null,
            ViewCount          = 0,
            CreatedAt          = now,
            UpdatedAt          = now,
            CreatedBy          = createdByUserId,
        };
    }

    /// <summary>True once <see cref="RevokedAt"/> has been set.</summary>
    public bool IsRevoked => RevokedAt.HasValue;

    /// <summary>True once the current time is at or past <see cref="ExpiresAt"/>.</summary>
    public bool IsExpired(DateTime utcNow) => utcNow >= ExpiresAt;

    /// <summary>
    /// A link is usable only while it is not soft-deleted, not revoked, and not expired.
    /// The public endpoint (US-178) uses this to decide between serving the job view and 410 Gone.
    /// </summary>
    public bool IsValid(DateTime utcNow) => !IsDeleted && !IsRevoked && !IsExpired(utcNow);

    /// <summary>
    /// Revokes the link immediately. Idempotent — revoking an already-revoked link is a no-op.
    /// </summary>
    public void Revoke()
    {
        if (IsRevoked) return;

        RevokedAt = DateTime.UtcNow;
        UpdatedAt = RevokedAt.Value;
    }

    /// <summary>
    /// Records one public view. Called by the public job endpoint (US-178) on each resolve.
    /// </summary>
    public void IncrementViewCount()
    {
        ViewCount++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Produces a 256-bit URL-safe token using a cryptographically-secure RNG.
    /// Base64url (RFC 4648 §5) keeps the value safe inside a path segment with no escaping.
    /// </summary>
    private static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenByteLength);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
