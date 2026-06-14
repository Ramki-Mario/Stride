namespace STRIDE.Modules.Identity.Application.Queries.GetRole;

public sealed record RoleDetailDto(
    Guid                        Id,
    string                      Name,
    string                      Description,
    bool                        IsSystemRole,
    IReadOnlyList<PermissionDto> Permissions);

public sealed record PermissionDto(Guid Id, string Key, string Description);
