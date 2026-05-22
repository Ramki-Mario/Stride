using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.BuildingBlocks.Infrastructure.Persistence;
using STRIDE.Modules.Invoicing.Application.Abstractions;
using STRIDE.Modules.Invoicing.Application.DTOs;

namespace STRIDE.Modules.Invoicing.Infrastructure.ReadModels;

internal sealed class InvoiceReadService : IInvoiceReadService
{
    private static readonly string SqlGetInvoices =
        SqlLoader.Load(typeof(InvoiceReadService).Assembly,
            "STRIDE.Modules.Invoicing.Infrastructure.ReadModels.Queries.GetInvoices.sql");

    private static readonly string SqlCountInvoices =
        SqlLoader.Load(typeof(InvoiceReadService).Assembly,
            "STRIDE.Modules.Invoicing.Infrastructure.ReadModels.Queries.CountInvoices.sql");

    private static readonly string SqlGetById =
        SqlLoader.Load(typeof(InvoiceReadService).Assembly,
            "STRIDE.Modules.Invoicing.Infrastructure.ReadModels.Queries.GetInvoiceById.sql");

    private static readonly string[] StatusLabels = ["Draft", "Sent", "Paid", "Void"];

    private readonly string _connectionString;

    public InvoiceReadService(IConfiguration configuration)
        => _connectionString = configuration.GetConnectionString("DefaultConnection")
           ?? throw new InvalidOperationException("DefaultConnection is not configured.");

    public async Task<PagedResult<InvoiceSummaryDto>> GetInvoicesAsync(
        Guid tenantId, string? search, int? status,
        int page, int pageSize, CancellationToken ct = default)
    {
        var param = new
        {
            TenantId = tenantId,
            Search   = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
            Status   = status,
            Offset   = (page - 1) * pageSize,
            PageSize = pageSize,
        };

        await using var conn = new SqlConnection(_connectionString);

        var total = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(SqlCountInvoices, param, cancellationToken: ct));

        if (total == 0)
            return PagedResult<InvoiceSummaryDto>.Empty(page, pageSize);

        var rows = await conn.QueryAsync<dynamic>(
            new CommandDefinition(SqlGetInvoices, param, cancellationToken: ct));

        var items = rows.Select(r => new InvoiceSummaryDto(
            Id:            (Guid)r.Id,
            InvoiceNumber: (string)r.InvoiceNumber,
            ClientName:    (string)r.ClientName,
            ClientEmail:   (string)r.ClientEmail,
            Currency:      (string)r.Currency,
            Status:        (int)r.Status,
            StatusLabel:   StatusLabels[(int)r.Status],
            DueDate:       DateOnly.FromDateTime((DateTime)r.DueDate),
            TotalAmount:   (decimal)r.TotalAmount,
            CreatedAt:     (DateTime)r.CreatedAt,
            SentAt:        r.SentAt is DBNull ? null : (DateTime?)r.SentAt,
            PaidAt:        r.PaidAt is DBNull ? null : (DateTime?)r.PaidAt
        )).ToList();

        return new PagedResult<InvoiceSummaryDto>(items, total, page, pageSize);
    }

    public async Task<InvoiceDetailDto?> GetInvoiceByIdAsync(
        Guid tenantId, Guid invoiceId, CancellationToken ct = default)
    {
        var param = new { TenantId = tenantId, InvoiceId = invoiceId };

        await using var conn = new SqlConnection(_connectionString);

        var rows = (await conn.QueryAsync<dynamic>(
            new CommandDefinition(SqlGetById, param, cancellationToken: ct))).ToList();

        if (rows.Count == 0) return null;

        var first = rows[0];
        int statusInt = (int)first.Status;

        var lineItems = rows
            .Where(r => r.LineItemId is not null && !(r.LineItemId is DBNull))
            .Select(r => new InvoiceLineItemDto(
                Id:          (Guid)r.LineItemId,
                Description: (string)r.LineItemDescription,
                UnitPrice:   (decimal)r.LineItemUnitPrice,
                Quantity:    (int)r.LineItemQuantity,
                Subtotal:    Math.Round((decimal)r.LineItemUnitPrice * (int)r.LineItemQuantity, 2)))
            .ToList();

        return new InvoiceDetailDto(
            Id:            (Guid)first.Id,
            InvoiceNumber: (string)first.InvoiceNumber,
            ClientName:    (string)first.ClientName,
            ClientEmail:   (string)first.ClientEmail,
            Currency:      (string)first.Currency,
            Status:        statusInt,
            StatusLabel:   StatusLabels[statusInt],
            DueDate:       DateOnly.FromDateTime((DateTime)first.DueDate),
            Notes:         first.Notes is DBNull ? null : (string?)first.Notes,
            TotalAmount:   lineItems.Sum(l => l.Subtotal),
            CreatedAt:     (DateTime)first.CreatedAt,
            SentAt:        first.SentAt is DBNull ? null : (DateTime?)first.SentAt,
            PaidAt:        first.PaidAt is DBNull ? null : (DateTime?)first.PaidAt,
            LineItems:     lineItems);
    }
}
