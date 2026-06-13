-- Revenue totals grouped by workflow type (definition name) for Sent/Paid invoices.
SELECT
    ISNULL(wi.WorkflowName, 'Unknown Workflow')  AS WorkflowType,
    SUM(li.UnitPrice * li.Quantity)              AS TotalAmount,
    COUNT(DISTINCT i.Id)                         AS InvoiceCount
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
GROUP BY ISNULL(wi.WorkflowName, 'Unknown Workflow')
ORDER BY TotalAmount DESC;
