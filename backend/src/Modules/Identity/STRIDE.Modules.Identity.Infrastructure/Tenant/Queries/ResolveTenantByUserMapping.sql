SELECT utm.TenantId
FROM   [identity].UserTenantMappings utm
INNER JOIN [identity].Users u ON u.Id = utm.UserId
WHERE  u.NormalizedEmail = @NormalizedEmail
  AND  u.IsDeleted = 0
  AND  utm.IsDeleted = 0
