SELECT
    c.Id                  AS CheckoutId,
    ki.Id                 AS KitItemId,
    ki.Name               AS KitItemName,
    ki.Category           AS Category,
    c.CreatedAt           AS CheckedOutAt,
    c.ExpectedReturnAt    AS ExpectedReturnAt,
    c.ReturnedAt          AS ReturnedAt,
    c.Notes               AS Notes,
    CASE c.Status
        WHEN 0 THEN 'Active'
        WHEN 1 THEN 'Returned'
        WHEN 2 THEN 'Overdue'
        ELSE        'Unknown'
    END                   AS StatusLabel,
    c.Status              AS StatusValue
FROM   [kitops].[KitCheckouts] c
JOIN   [kitops].[KitItems]     ki ON ki.Id = c.KitItemId
WHERE  c.TenantId          = @TenantId
  AND  c.CheckedOutByUserId = @UserId
  AND  c.IsDeleted          = 0
ORDER  BY c.CreatedAt DESC;
