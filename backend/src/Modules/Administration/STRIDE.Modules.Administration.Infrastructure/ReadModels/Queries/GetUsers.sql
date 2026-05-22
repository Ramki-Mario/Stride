-- Administration: paginated user list for a tenant.
-- Joins identity.Users → identity.UserRoles → identity.Roles to surface the
-- user's primary role.  Supports free-text search on DisplayName / Email,
-- a role name filter, and a status filter (Active / Pending / Inactive).
--
-- Parameters: @TenantId, @Search (nullable), @Role (nullable), @Status (nullable),
--             @Offset, @PageSize

SELECT
    u.Id,
    u.DisplayName,
    u.Email,
    COALESCE(r.Name, 'Member')  AS Role,
    u.IsActive,
    u.IsPending,
    u.CreatedAt
FROM  identity.Users      u
LEFT JOIN identity.UserRoles   ur ON ur.UserId   = u.Id
                                 AND ur.TenantId  = u.TenantId
                                 AND ur.IsDeleted = 0
LEFT JOIN identity.Roles       r  ON r.Id         = ur.RoleId
                                 AND r.IsDeleted   = 0
WHERE u.TenantId  = @TenantId
  AND u.IsDeleted = 0
  AND (@Search IS NULL
       OR u.DisplayName LIKE '%' + @Search + '%'
       OR u.Email       LIKE '%' + @Search + '%')
  AND (@Role   IS NULL OR r.Name = @Role)
  AND (@Status IS NULL
       OR (@Status = 'Active'   AND u.IsActive = 1 AND u.IsPending = 0)
       OR (@Status = 'Pending'  AND u.IsPending = 1)
       OR (@Status = 'Inactive' AND u.IsActive = 0 AND u.IsPending = 0))
ORDER BY u.CreatedAt DESC
OFFSET     @Offset ROWS
FETCH NEXT @PageSize ROWS ONLY;
