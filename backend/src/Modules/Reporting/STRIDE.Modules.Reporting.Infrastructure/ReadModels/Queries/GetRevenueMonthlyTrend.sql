-- Zero-filled monthly revenue trend for workflow-linked invoices (Sent or Paid).
-- A recursive CTE generates every calendar month in the range so months with
-- no invoices appear as 0 rather than being absent from the result.
WITH Months AS (
    SELECT DATEFROMPARTS(YEAR(@FromDate), MONTH(@FromDate), 1) AS MonthStart
    UNION ALL
    SELECT DATEADD(MONTH, 1, MonthStart)
    FROM   Months
    WHERE  DATEADD(MONTH, 1, MonthStart)
               <= DATEFROMPARTS(YEAR(@ToDate), MONTH(@ToDate), 1)
)
SELECT
    m.MonthStart,
    ISNULL(SUM(li.UnitPrice * li.Quantity), 0)
                                                               AS InvoicedAmount,
    ISNULL(SUM(CASE WHEN i.Status = 2 THEN li.UnitPrice * li.Quantity ELSE 0 END), 0)
                                                               AS PaidAmount
FROM Months m
LEFT JOIN [invoicing].[Invoices] i
    ON  i.TenantId                 = @TenantId
    AND i.IsDeleted                = 0
    AND i.Status                  IN (1, 2)
    AND i.SourceWorkflowInstanceId IS NOT NULL
    AND DATEFROMPARTS(YEAR(i.SentAt), MONTH(i.SentAt), 1) = m.MonthStart
LEFT JOIN [invoicing].[InvoiceLineItems] li
    ON  li.InvoiceId = i.Id
GROUP BY m.MonthStart
ORDER BY m.MonthStart
OPTION (MAXRECURSION 24);
