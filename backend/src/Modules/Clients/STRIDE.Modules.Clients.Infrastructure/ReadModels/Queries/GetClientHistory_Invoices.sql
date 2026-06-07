-- Returns invoices linked to the given client (cross-schema read).
SELECT
    i.Id            AS Id,
    i.InvoiceNumber AS InvoiceNumber,
    i.Status        AS Status,
    i.Currency      AS Currency,
    i.CreatedAt     AS CreatedAt,
    -- TotalAmount is computed in the domain from line items, so sum here
    ISNULL((
        SELECT SUM(il.UnitPrice * il.Quantity)
        FROM   invoicing.InvoiceLineItems il
        WHERE  il.InvoiceId = i.Id
    ), 0) AS TotalAmount
FROM invoicing.Invoices i
WHERE i.TenantId  = @TenantId
  AND i.ClientId  = @ClientId
  AND i.IsDeleted = 0
ORDER BY i.CreatedAt DESC;
