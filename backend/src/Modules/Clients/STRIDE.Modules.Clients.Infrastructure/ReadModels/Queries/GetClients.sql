SELECT
    c.Id            AS Id,
    c.Name          AS Name,
    c.ContactPerson AS ContactPerson,
    c.Email         AS Email,
    c.Phone         AS Phone,
    c.Status        AS Status,
    c.CreatedAt     AS CreatedAt
FROM clients.Clients c
WHERE c.TenantId = @TenantId
  AND c.IsDeleted = 0
  AND (@Status IS NULL OR c.Status = @Status)
  AND (@Search IS NULL OR c.Name          LIKE '%' + @Search + '%'
                       OR c.ContactPerson LIKE '%' + @Search + '%'
                       OR c.Email         LIKE '%' + @Search + '%'
                       OR c.Phone         LIKE '%' + @Search + '%')
ORDER BY c.Name ASC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
