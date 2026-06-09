SELECT
    t.Id,
    t.Name,
    t.Description,
    t.ParentTeamId,
    p.Name AS ParentTeamName,
    t.[Status],
    t.CreatedAt,
    t.UpdatedAt
FROM   [teams].[Teams] t
LEFT   JOIN [teams].[Teams] p
       ON   p.Id = t.ParentTeamId AND p.IsDeleted = 0
WHERE  t.TenantId  = @TenantId
  AND  t.Id        = @TeamId
  AND  t.IsDeleted = 0;
