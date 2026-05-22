SELECT
    i.Id             AS Id,
    i.InvoiceNumber  AS InvoiceNumber,
    i.ClientName     AS ClientName,
    i.ClientEmail    AS ClientEmail,
    i.Currency       AS Currency,
    i.Status         AS Status,
    i.DueDate        AS DueDate,
    i.Notes          AS Notes,
    i.SentAt         AS SentAt,
    i.PaidAt         AS PaidAt,
    i.CreatedAt      AS CreatedAt,
    ISNULL(SUM(l.UnitPrice * l.Quantity), 0) AS TotalAmount
FROM invoicing.Invoices i
LEFT JOIN invoicing.InvoiceLineItems l ON l.InvoiceId = i.Id
WHERE i.TenantId = @TenantId
  AND i.IsDeleted = 0
  AND (@Status IS NULL OR i.Status = @Status)
  AND (@Search IS NULL OR i.InvoiceNumber LIKE '%' + @Search + '%'
                       OR i.ClientName    LIKE '%' + @Search + '%'
                       OR i.ClientEmail   LIKE '%' + @Search + '%')
GROUP BY
    i.Id, i.InvoiceNumber, i.ClientName, i.ClientEmail, i.Currency,
    i.Status, i.DueDate, i.Notes, i.SentAt, i.PaidAt, i.CreatedAt
ORDER BY i.CreatedAt DESC
OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
