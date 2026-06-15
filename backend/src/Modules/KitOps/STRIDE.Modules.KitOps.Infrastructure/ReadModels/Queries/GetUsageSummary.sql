SELECT
    ki.Id                                           AS KitItemId,
    ki.Name                                         AS Name,
    ki.Category                                     AS Category,
    ki.TotalQuantity                                AS TotalQuantity,
    COUNT(c.Id)                                     AS TotalCheckouts,
    SUM(CASE WHEN c.Status = 0 THEN 1 ELSE 0 END)  AS ActiveCheckouts,
    SUM(CASE WHEN c.Status = 2 THEN 1 ELSE 0 END)  AS OverdueCheckouts,
    ISNULL((
        SELECT COUNT(1)
        FROM   [kitops].[KitReservations] r
        WHERE  r.KitItemId  = ki.Id
          AND  r.TenantId   = ki.TenantId
          AND  r.Status     = 0
          AND  r.IsDeleted  = 0
    ), 0)                                           AS PendingReservations,
    AVG(CASE
        WHEN c.Status = 1
        THEN CAST(DATEDIFF(MINUTE, c.CreatedAt, c.ReturnedAt) AS FLOAT) / 1440.0
        ELSE NULL
    END)                                            AS AvgDaysCheckedOut
FROM   [kitops].[KitItems] ki
LEFT JOIN [kitops].[KitCheckouts] c
    ON  c.KitItemId  = ki.Id
    AND c.TenantId   = ki.TenantId
    AND c.IsDeleted  = 0
    AND (@From IS NULL OR c.CreatedAt >= @From)
    AND (@To   IS NULL OR c.CreatedAt <= @To)
WHERE  ki.TenantId  = @TenantId
  AND  ki.IsDeleted = 0
GROUP  BY ki.Id, ki.Name, ki.Category, ki.TotalQuantity
ORDER  BY ki.Name;
