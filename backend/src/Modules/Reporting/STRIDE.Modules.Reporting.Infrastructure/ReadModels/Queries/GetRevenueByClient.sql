-- Revenue totals per client for workflow-linked Sent/Paid invoices, ranked by total.
SELECT
    i.ClientName,
    SUM(li.UnitPrice * li.Quantity)                                              AS TotalAmount,
    ISNULL(SUM(CASE WHEN i.Status = 2 THEN li.UnitPrice * li.Quantity ELSE 0 END), 0)
                                                                                 AS PaidAmount,
    COUNT(DISTINCT i.Id)                                                          AS InvoiceCount
FROM [invoicing].[Invoices] i
INNER JOIN [invoicing].[InvoiceLineItems] li
    ON  li.InvoiceId = i.Id
WHERE i.TenantId                 = @TenantId
  AND i.IsDeleted                = 0
  AND i.Status                  IN (1, 2)
  AND i.SourceWorkflowInstanceId IS NOT NULL
  AND i.SentAt                  >= @FromDate
  AND i.SentAt                  <= @ToDate
GROUP BY i.ClientName
ORDER BY TotalAmount DESC;
