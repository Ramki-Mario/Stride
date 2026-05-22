SELECT COUNT(*)
FROM invoicing.Invoices i
WHERE i.TenantId = @TenantId
  AND i.IsDeleted = 0
  AND (@Status IS NULL OR i.Status = @Status)
  AND (@Search IS NULL OR i.InvoiceNumber LIKE '%' + @Search + '%'
                       OR i.ClientName    LIKE '%' + @Search + '%'
                       OR i.ClientEmail   LIKE '%' + @Search + '%')
