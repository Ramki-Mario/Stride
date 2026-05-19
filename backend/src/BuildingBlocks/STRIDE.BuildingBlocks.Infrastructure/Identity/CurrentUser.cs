using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using STRIDE.BuildingBlocks.Application.Abstractions;

namespace STRIDE.BuildingBlocks.Infrastructure.Identity;

/// <summary>
/// Reads the current authenticated user's identity from the ambient
/// <see cref="IHttpContextAccessor"/>. Populated from the validated JWT
/// claims set by AddJwtBearer on STRIDE.Host (US-025).
///
/// Claim mapping mirrors JwtTokenService (MapInboundClaims = false keeps
/// short JWT names — no silent rename to long Microsoft URIs):
///   "sub"   → UserId
///   "email" → Email  (NOT ClaimTypes.Email — that URI is never set with mapping off)
///   ClaimTypes.Role → Roles[] (stored with full URI by JwtTokenService, unchanged)
/// </summary>
internal sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public Guid UserId
    {
        get
        {
            var sub = Principal?.FindFirstValue("sub");
            return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
        }
    }

    // "email" is the short JWT claim name.  With MapInboundClaims = false in
    // AddJwtBearer, the token's "email" field is never remapped to the long
    // ClaimTypes.Email URI, so we must look it up by its original short name.
    public string Email =>
        Principal?.FindFirstValue("email") ?? string.Empty;

    public IReadOnlyList<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role)
                  .Select(c => c.Value)
                  .ToList()
                  .AsReadOnly()
        ?? (IReadOnlyList<string>)[];

    public bool IsAuthenticated =>
        Principal?.Identity?.IsAuthenticated ?? false;
}
