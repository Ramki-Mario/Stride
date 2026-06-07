SELECT
    c.Id            AS Id,
    c.Name          AS Name,
    c.ContactPerson AS ContactPerson,
    c.Email         AS Email,
    c.Phone         AS Phone,
    c.Address       AS Address,
    c.Notes         AS Notes,
    c.Status        AS Status,
    c.CreatedAt     AS CreatedAt,
    c.UpdatedAt     AS UpdatedAt
FROM clients.Clients c
WHERE c.TenantId = @TenantId
  AND c.Id       = @ClientId
  AND c.IsDeleted = 0
