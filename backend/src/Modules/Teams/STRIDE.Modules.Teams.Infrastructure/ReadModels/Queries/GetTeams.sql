SELECT
    t.Id,
    t.Name,
    t.Description,
    t.ParentTeamId,
    p.Name AS ParentTeamName,
    t.[Status],
    t.CreatedAt
FROM   [teams].[Teams] t
LEFT   JOIN [teams].[Teams] p
       ON   p.Id = t.ParentTeamId AND p.IsDeleted = 0
WHERE  t.TenantId  = @TenantId
  AND  t.IsDeleted = 0
  AND  (@Status IS NULL OR t.[Status] = @Status)
  AND  (
         @Search IS NULL
         OR t.Name        LIKE '%' + @Search + '%'
         OR t.Description LIKE '%' + @Search + '%'
       )
ORDER  BY t.Name
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
