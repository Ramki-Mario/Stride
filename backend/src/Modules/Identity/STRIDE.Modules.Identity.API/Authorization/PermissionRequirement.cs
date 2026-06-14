using Microsoft.AspNetCore.Authorization;

namespace STRIDE.Modules.Identity.API.Authorization;

/// <summary>
/// Authorization requirement carrying a single permission key (e.g. <c>workflow.create</c>).
/// Satisfied by <see cref="PermissionAuthorizationHandler"/> when the current user's
/// effective permission set contains the key.
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string PermissionKey { get; }

    public PermissionRequirement(string permissionKey)
        => PermissionKey = permissionKey;
}
