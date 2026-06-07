SELECT COUNT(*)
FROM clients.Clients c
WHERE c.TenantId = @TenantId
  AND c.IsDeleted = 0
  AND (@Status IS NULL OR c.Status = @Status)
  AND (@Search IS NULL OR c.Name          LIKE '%' + @Search + '%'
                       OR c.ContactPerson LIKE '%' + @Search + '%'
                       OR c.Email         LIKE '%' + @Search + '%'
                       OR c.Phone         LIKE '%' + @Search + '%')
