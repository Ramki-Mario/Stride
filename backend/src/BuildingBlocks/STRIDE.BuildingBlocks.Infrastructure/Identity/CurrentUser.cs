using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using STRIDE.BuildingBlocks.Application.Abstractions;

namespace STRIDE.BuildingBlocks.Infrastructure.Identity;

/// <summary>
/// Reads the current authenticated user's identity from the ambient
/// <see cref="IHttpContextAccessor"/>. Populated from the validated JWT
/// claims set by AddJwtBearer on STRIDE.Host (US-025).
///
/// Claim mapping mirrors JwtTokenService:
///   sub  → UserId
///   email (ClaimTypes.Email) → Email
///   role (ClaimTypes.Role)   → Roles[]
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

    public string Email =>
        Principal?.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

    public IReadOnlyList<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role)
                  .Select(c => c.Value)
                  .ToList()
                  .AsReadOnly()
        ?? (IReadOnlyList<string>)[];

    public bool IsAuthenticated =>
        Principal?.Identity?.IsAuthenticated ?? false;
}
