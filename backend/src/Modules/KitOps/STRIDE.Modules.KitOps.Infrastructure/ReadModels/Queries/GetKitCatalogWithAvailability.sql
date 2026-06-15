SELECT
    ki.Id                                                             AS KitItemId,
    ki.Name                                                           AS Name,
    ki.Category                                                       AS Category,
    ki.Description                                                    AS Description,
    ki.TotalQuantity                                                  AS TotalQuantity,
    ISNULL(c.Outstanding, 0)                                          AS OutstandingCheckouts,
    CASE
        WHEN ki.TotalQuantity - ISNULL(c.Outstanding, 0) < 0 THEN 0
        ELSE ki.TotalQuantity - ISNULL(c.Outstanding, 0)
    END                                                               AS AvailableQuantity,
    ISNULL(c.Overdue, 0)                                             AS OverdueCheckouts
FROM  [kitops].[KitItems] ki
LEFT JOIN (
    SELECT
        KitItemId,
        SUM(CASE WHEN Status IN (0, 2) THEN 1 ELSE 0 END) AS Outstanding,
        SUM(CASE WHEN Status = 2       THEN 1 ELSE 0 END) AS Overdue
    FROM  [kitops].[KitCheckouts]
    WHERE TenantId = @TenantId
      AND IsDeleted = 0
    GROUP BY KitItemId
) c ON c.KitItemId = ki.Id
WHERE ki.TenantId = @TenantId
  AND ki.IsDeleted = 0
  AND ki.IsActive  = 1
ORDER BY ki.Name;
