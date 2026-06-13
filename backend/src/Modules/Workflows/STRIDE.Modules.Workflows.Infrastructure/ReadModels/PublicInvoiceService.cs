using Dapper;
using STRIDE.BuildingBlocks.Infrastructure.Persistence;
using STRIDE.Modules.Workflows.Application.Abstractions;
using STRIDE.Modules.Workflows.Application.DTOs;

namespace STRIDE.Modules.Workflows.Infrastructure.ReadModels;

/// <summary>
/// Cross-schema Dapper read for the public invoice preview (US-179).
/// Queries invoicing.Invoices directly — no tenant filter needed because the
/// workflow instance ID is already tenant-scoped when the caller resolves it.
/// </summary>
internal sealed class PublicInvoiceService : IPublicInvoiceService
{
    private static readonly string[] StatusLabels = ["Draft", "Sent", "Paid", "Void"];

    private static readonly string Sql =
        SqlLoader.Load(typeof(PublicInvoiceService).Assembly,
            "STRIDE.Modules.Workflows.Infrastructure.ReadModels.Queries.GetPublicInvoiceByWorkflowInstanceId.sql");

    private readonly IDbConnectionFactory _db;

    public PublicInvoiceService(IDbConnectionFactory db) => _db = db;

    public async Task<PublicInvoiceDto?> GetByWorkflowInstanceIdAsync(
        Guid workflowInstanceId,
        CancellationToken cancellationToken = default)
    {
        await using var conn = await _db.OpenConnectionAsync(cancellationToken);

        var rows = (await conn.QueryAsync<dynamic>(
            new CommandDefinition(Sql,
                new { WorkflowInstanceId = workflowInstanceId },
                cancellationToken: cancellationToken))).ToList();

        if (rows.Count == 0) return null;

        var first     = rows[0];
        int statusInt = (int)first.Status;

        var lineItems = rows
            .Where(r => r.LineItemId is not null && !(r.LineItemId is DBNull))
            .Select(r => new PublicInvoiceLineItemDto(
                Description: (string)r.LineItemDescription,
                Subtotal:    Math.Round((decimal)r.LineItemUnitPrice * (int)r.LineItemQuantity, 2)))
            .ToList();

        return new PublicInvoiceDto(
            InvoiceNumber: (string)first.InvoiceNumber,
            StatusLabel:   statusInt < StatusLabels.Length ? StatusLabels[statusInt] : statusInt.ToString(),
            Currency:      (string)first.Currency,
            TotalAmount:   lineItems.Sum(l => l.Subtotal),
            DueDate:       DateOnly.FromDateTime((DateTime)first.DueDate),
            LineItems:     lineItems.AsReadOnly());
    }
}
