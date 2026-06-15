SELECT
    Id,
    Name,
    Category,
    Description,
    TotalQuantity,
    IsActive,
    CreatedAt
FROM   [kitops].[KitItems]
WHERE  TenantId  = @TenantId
  AND  IsDeleted = 0
  AND  (@IsActive IS NULL OR IsActive = @IsActive)
  AND  (@Category IS NULL OR Category = @Category)
  AND  (
         @Search IS NULL
         OR Name        LIKE '%' + @Search + '%'
         OR Category    LIKE '%' + @Search + '%'
         OR Description LIKE '%' + @Search + '%'
       )
ORDER  BY Name
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
