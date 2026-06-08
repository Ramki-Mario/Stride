SELECT COUNT(*)
FROM   [teams].[Teams]
WHERE  TenantId  = @TenantId
  AND  IsDeleted = 0
  AND  (@Status IS NULL OR [Status] = @Status)
  AND  (
         @Search IS NULL
         OR Name LIKE '%' + @Search + '%'
         OR Description LIKE '%' + @Search + '%'
       );
