SELECT
    i.Id                       AS Id,
    i.InvoiceNumber            AS InvoiceNumber,
    i.ClientName               AS ClientName,
    i.ClientEmail              AS ClientEmail,
    i.Currency                 AS Currency,
    i.Status                   AS Status,
    i.DueDate                  AS DueDate,
    i.Notes                    AS Notes,
    i.SentAt                   AS SentAt,
    i.PaidAt                   AS PaidAt,
    i.CreatedAt                AS CreatedAt,
    i.SourceWorkflowInstanceId AS SourceWorkflowInstanceId,
    l.Id                       AS LineItemId,
    l.Description              AS LineItemDescription,
    l.UnitPrice                AS LineItemUnitPrice,
    l.Quantity                 AS LineItemQuantity
FROM invoicing.Invoices i
LEFT JOIN invoicing.InvoiceLineItems l ON l.InvoiceId = i.Id
WHERE i.TenantId = @TenantId
  AND i.Id       = @InvoiceId
  AND i.IsDeleted = 0
