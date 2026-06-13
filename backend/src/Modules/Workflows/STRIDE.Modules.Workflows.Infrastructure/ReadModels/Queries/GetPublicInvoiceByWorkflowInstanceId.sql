WITH LatestInvoice AS (
    SELECT TOP 1
        i.Id, i.InvoiceNumber, i.Status, i.Currency, i.DueDate
    FROM invoicing.Invoices i
    WHERE i.SourceWorkflowInstanceId = @WorkflowInstanceId
      AND i.IsDeleted = 0
    ORDER BY i.CreatedAt DESC
)
SELECT
    li.InvoiceNumber    AS InvoiceNumber,
    li.Status           AS Status,
    li.Currency         AS Currency,
    li.DueDate          AS DueDate,
    l.Id                AS LineItemId,
    l.Description       AS LineItemDescription,
    l.UnitPrice         AS LineItemUnitPrice,
    l.Quantity          AS LineItemQuantity
FROM LatestInvoice li
LEFT JOIN invoicing.InvoiceLineItems l ON l.InvoiceId = li.Id
