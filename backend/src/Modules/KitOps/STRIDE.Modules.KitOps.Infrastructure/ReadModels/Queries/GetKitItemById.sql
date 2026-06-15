SELECT
    Id,
    Name,
    Category,
    Description,
    TotalQuantity,
    IsActive,
    CreatedAt,
    UpdatedAt
FROM   [kitops].[KitItems]
WHERE  TenantId  = @TenantId
  AND  Id        = @KitItemId
  AND  IsDeleted = 0;
