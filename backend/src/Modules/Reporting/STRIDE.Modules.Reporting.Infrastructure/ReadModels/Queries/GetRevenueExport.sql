-- Flat invoice-level export for revenue analytics (Sent/Paid, workflow-linked).
SELECT
    i.InvoiceNumber,
    i.ClientName,
    ISNULL(wi.WorkflowName, '')                  AS WorkflowType,
    i.Currency,
    SUM(li.UnitPrice * li.Quantity)              AS TotalAmount,
    CASE i.Status WHEN 1 THEN 'Sent' WHEN 2 THEN 'Paid' ELSE '' END
                                                 AS Status,
    i.SentAt
FROM [invoicing].[Invoices] i
INNER JOIN [invoicing].[InvoiceLineItems] li
    ON  li.InvoiceId = i.Id
LEFT JOIN [workflows].[WorkflowInstances] wi
    ON  wi.Id = i.SourceWorkflowInstanceId
WHERE i.TenantId                 = @TenantId
  AND i.IsDeleted                = 0
  AND i.Status                  IN (1, 2)
  AND i.SourceWorkflowInstanceId IS NOT NULL
  AND i.SentAt                  >= @FromDate
  AND i.SentAt                  <= @ToDate
GROUP BY i.Id, i.InvoiceNumber, i.ClientName, wi.WorkflowName,
         i.Currency, i.Status, i.SentAt
ORDER BY i.SentAt DESC;
