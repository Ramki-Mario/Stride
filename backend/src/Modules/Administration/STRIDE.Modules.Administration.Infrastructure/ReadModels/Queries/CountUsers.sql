-- Count-companion for GetUsers.sql — returns total matching users for pagination.
-- Mirrors the CTE join so the role-name filter stays consistent.
--
-- Parameters: @TenantId, @Search (nullable), @Role (nullable), @Status (nullable)

WITH RankedRoles AS (
    SELECT
        ur.UserId,
        r.Name AS RoleName,
        ROW_NUMBER() OVER (PARTITION BY ur.UserId ORDER BY ur.CreatedAt DESC) AS rn
    FROM  [identity].[UserRoles] ur
    JOIN  [identity].[Roles]     r  ON r.Id        = ur.RoleId
                                   AND r.IsDeleted  = 0
    WHERE ur.IsDeleted = 0
      AND ur.TenantId  = @TenantId
)
SELECT COUNT(*)
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
       OR (@Status = 'Inactive' AND u.IsActive = 0 AND u.IsPending = 0));
