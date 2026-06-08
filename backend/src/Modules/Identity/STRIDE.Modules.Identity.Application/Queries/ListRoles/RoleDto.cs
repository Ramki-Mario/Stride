namespace STRIDE.Modules.Identity.Application.Queries.ListRoles;

/// <summary>Lightweight role read model used for dropdowns and lookups.</summary>
public sealed record RoleDto(
    Guid   Id,
    string Name,
    string Description);
