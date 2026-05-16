SELECT TenantId
FROM   [identity].TenantDomainMappings
WHERE  CorporateDomain = @Domain
  AND  IsDeleted = 0
