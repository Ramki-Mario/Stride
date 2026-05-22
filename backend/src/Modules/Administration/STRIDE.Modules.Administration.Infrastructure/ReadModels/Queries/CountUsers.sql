-- Count-companion for GetUsers.sql — returns total matching rows for pagination.
SELECT COUNT(DISTINCT u.Id)
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
       OR (@Status = 'Inactive' AND u.IsActive = 0 AND u.IsPending = 0));
