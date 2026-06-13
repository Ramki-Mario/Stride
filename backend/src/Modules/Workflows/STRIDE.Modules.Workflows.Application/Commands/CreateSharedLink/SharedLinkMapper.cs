using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Application.Commands.CreateSharedLink;

/// <summary>
/// Maps a <see cref="SharedWorkflowLink"/> to its management-side DTO, computing the
/// public URL and validity flags relative to "now". Shared by the create command and
/// the list query so the shape stays consistent.
/// </summary>
internal static class SharedLinkMapper
{
    public static SharedLinkDto ToDto(SharedWorkflowLink link, ISharedLinkUrlBuilder urlBuilder)
    {
        var now = DateTime.UtcNow;
        return new SharedLinkDto(
            Id:        link.Id,
            Token:     link.Token,
            ShareUrl:  urlBuilder.BuildShareUrl(link.Token),
            ExpiresAt: link.ExpiresAt,
            RevokedAt: link.RevokedAt,
            ViewCount: link.ViewCount,
            IsExpired: link.IsExpired(now),
            IsActive:  link.IsValid(now),
            CreatedAt: link.CreatedAt);
    }
}
