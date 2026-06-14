-- Administration: paginated user list for a tenant.
-- Uses a CTE to pick the latest active custom role per user so that users with
-- multiple role assignments appear exactly once.  Supports free-text search on
-- DisplayName / Email, a role name filter, and a status filter.
--
-- Parameters: @TenantId, @Search (nullable), @Role (nullable), @Status (nullable),
--             @Offset, @PageSize

WITH RankedRoles AS (
    SELECT
        ur.UserId,
        r.Id   AS RoleId,
        r.Name AS RoleName,
        ROW_NUMBER() OVER (PARTITION BY ur.UserId ORDER BY ur.CreatedAt DESC) AS rn
    FROM  [identity].[UserRoles] ur
    JOIN  [identity].[Roles]     r  ON r.Id        = ur.RoleId
                                   AND r.IsDeleted  = 0
    WHERE ur.IsDeleted = 0
      AND ur.TenantId  = @TenantId
)
SELECT
    u.Id,
    u.DisplayName,
    u.Email,
    rr.RoleId,
    COALESCE(rr.RoleName, 'No Role') AS Role,
    u.IsActive,
    u.IsPending,
    u.CreatedAt
FROM  [identity].[Users] u
LEFT JOIN RankedRoles rr ON rr.UserId = u.Id AND rr.rn = 1
WHERE u.TenantId  = @TenantId
  AND u.IsDeleted = 0
  AND (@Search IS NULL
       OR u.DisplayName LIKE '%' + @Search + '%'
       OR u.Email       LIKE '%' + @Search + '%')
  AND (@Role IS NULL OR rr.RoleName = @Role)
  AND (@Status IS NULL
       OR (@Status = 'Active'   AND u.IsActive = 1 AND u.IsPending = 0)
       OR (@Status = 'Pending'  AND u.IsPending = 1)
       OR (@Status = 'Inactive' AND u.IsActive = 0 AND u.IsPending = 0))
ORDER BY u.CreatedAt DESC
OFFSET     @Offset ROWS
FETCH NEXT @PageSize ROWS ONLY;
