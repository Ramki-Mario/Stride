SELECT
    r.Id                AS ReservationId,
    ki.Id               AS KitItemId,
    ki.Name             AS KitItemName,
    ki.Category         AS Category,
    r.CreatedAt         AS RequestedAt,
    r.Notes             AS Notes,
    CASE r.Status
        WHEN 0 THEN 'Pending'
        WHEN 1 THEN 'Fulfilled'
        WHEN 2 THEN 'Cancelled'
        ELSE        'Unknown'
    END                 AS StatusLabel,
    r.Status            AS StatusValue
FROM   [kitops].[KitReservations] r
JOIN   [kitops].[KitItems]        ki ON ki.Id = r.KitItemId
WHERE  r.TenantId           = @TenantId
  AND  r.RequestedByUserId  = @UserId
  AND  r.IsDeleted           = 0
ORDER  BY r.CreatedAt DESC;
