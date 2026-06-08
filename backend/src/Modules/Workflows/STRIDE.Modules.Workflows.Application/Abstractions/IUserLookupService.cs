namespace STRIDE.Modules.Workflows.Application.Abstractions;

/// <summary>
/// Cross-module bridge: resolves user IDs from email local-parts (the part before '@')
/// without creating a hard project reference to the Identity module.
/// Implemented in Infrastructure via Dapper.
/// </summary>
public interface IUserLookupService
{
    /// <summary>
    /// Returns a map of <c>emailLocalPart → userId</c> for every active user in the
    /// tenant whose email local-part (case-insensitive) matches one of the supplied values.
    /// Local-parts that do not match any user are simply absent from the result.
    /// </summary>
    Task<IReadOnlyDictionary<string, Guid>> LookupByEmailLocalPartsAsync(
        Guid                    tenantId,
        IEnumerable<string>     localParts,
        CancellationToken       ct = default);
}
