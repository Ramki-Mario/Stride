SELECT
    c.Id                    AS CheckoutId,
    ki.Name                 AS KitItemName,
    ki.Category             AS Category,
    c.CheckedOutByUserId    AS CheckedOutByUserId,
    u.Email                 AS CheckedOutByEmail,
    c.CreatedAt             AS CheckedOutAt,
    c.ExpectedReturnAt      AS ExpectedReturnAt,
    c.ReturnedAt            AS ReturnedAt,
    CASE c.Status
        WHEN 0 THEN 'Active'
        WHEN 1 THEN 'Returned'
        WHEN 2 THEN 'Overdue'
        ELSE        'Unknown'
    END                     AS Status,
    DATEDIFF(
        DAY,
        c.CreatedAt,
        ISNULL(c.ReturnedAt, GETUTCDATE())
    )                       AS DaysCheckedOut
FROM   [kitops].[KitCheckouts]  c
JOIN   [kitops].[KitItems]      ki ON ki.Id       = c.KitItemId
LEFT JOIN [identity].[Users]    u  ON u.Id        = c.CheckedOutByUserId
                                   AND u.TenantId = c.TenantId
WHERE  c.TenantId  = @TenantId
  AND  c.IsDeleted = 0
  AND  (@KitItemId IS NULL OR c.KitItemId = @KitItemId)
  AND  (@From      IS NULL OR c.CreatedAt >= @From)
  AND  (@To        IS NULL OR c.CreatedAt <= @To)
ORDER  BY c.CreatedAt DESC;
