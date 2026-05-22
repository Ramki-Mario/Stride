using System.ComponentModel.DataAnnotations;

namespace STRIDE.Modules.Identity.API.Dtos;

/// <summary>Request body for <c>POST /api/identity/tenants/register</c>.</summary>
public sealed class RegisterTenantRequest
{
    [Required, StringLength(200)]
    public string OrgName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    public string Plan { get; set; } = "Starter";

    [Required, EmailAddress, StringLength(256)]
    public string AdminEmail { get; set; } = string.Empty;

    [Required, StringLength(256, MinimumLength = 8)]
    public string AdminPassword { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string AdminDisplayName { get; set; } = string.Empty;
}
