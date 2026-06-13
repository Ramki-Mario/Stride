namespace STRIDE.Modules.Reporting.Application.ReadModels;

/// <summary>Total invoiced and paid amounts for a single calendar month.</summary>
public sealed record RevenueMonthlyDto(
    DateOnly MonthStart,
    decimal  InvoicedAmount,
    decimal  PaidAmount);

/// <summary>Revenue aggregated by workflow type (definition name).</summary>
public sealed record RevenueByWorkflowTypeDto(
    string  WorkflowType,
    decimal TotalAmount,
    int     InvoiceCount);

/// <summary>Revenue aggregated by client name, ordered by total descending.</summary>
public sealed record RevenueByClientDto(
    string  ClientName,
    decimal TotalAmount,
    decimal PaidAmount,
    int     InvoiceCount);

/// <summary>High-level totals for the revenue summary strip.</summary>
public sealed record RevenueSummaryDto(
    decimal TotalInvoiced,
    decimal TotalPaid,
    decimal Outstanding);

/// <summary>Top-level response for the revenue analytics endpoint.</summary>
public sealed record RevenueAnalyticsDto(
    RevenueSummaryDto                    Summary,
    IReadOnlyList<RevenueMonthlyDto>     MonthlyTrend,
    IReadOnlyList<RevenueByWorkflowTypeDto> ByWorkflowType,
    IReadOnlyList<RevenueByClientDto>    ByClient);

/// <summary>Flat invoice row used for CSV export of revenue data.</summary>
public sealed record RevenueExportRowDto(
    string   InvoiceNumber,
    string   ClientName,
    string   WorkflowType,
    string   Currency,
    decimal  TotalAmount,
    string   Status,
    DateTime SentAt);
