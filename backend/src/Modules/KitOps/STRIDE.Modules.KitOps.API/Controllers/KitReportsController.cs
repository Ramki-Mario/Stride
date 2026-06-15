using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.KitOps.Application.Abstractions;
using STRIDE.Modules.KitOps.Application.Queries.GetCheckoutHistoryReport;
using STRIDE.Modules.KitOps.Application.Queries.GetKitUsageSummary;

namespace STRIDE.Modules.KitOps.API.Controllers;

/// <summary>
/// KitOps usage reports — JSON reads and Excel exports.
///
///   GET /api/kit-reports/checkout-history         — checkout history (JSON)
///   GET /api/kit-reports/checkout-history/export  — checkout history (Excel .xlsx)
///   GET /api/kit-reports/usage-summary            — per-item utilisation (JSON)
///   GET /api/kit-reports/usage-summary/export     — per-item utilisation (Excel .xlsx)
///
/// Admin-only; field users access their own activity via kit-checkouts endpoints.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/kit-reports")]
public sealed class KitReportsController : ControllerBase
{
    private readonly IMediator            _mediator;
    private readonly ITenantContext       _tenantContext;
    private readonly IKitExcelExportService _excel;

    public KitReportsController(
        IMediator mediator,
        ITenantContext tenantContext,
        IKitExcelExportService excel)
    {
        _mediator      = mediator;
        _tenantContext = tenantContext;
        _excel         = excel;
    }

    [HttpGet("checkout-history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCheckoutHistory(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid?     kitItemId,
        CancellationToken     cancellationToken)
    {
        var rows = await _mediator.Send(
            new GetCheckoutHistoryReportQuery(_tenantContext.TenantId, from, to, kitItemId),
            cancellationToken);

        return Ok(rows);
    }

    [HttpGet("checkout-history/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportCheckoutHistory(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid?     kitItemId,
        CancellationToken     cancellationToken)
    {
        var rows = await _mediator.Send(
            new GetCheckoutHistoryReportQuery(_tenantContext.TenantId, from, to, kitItemId),
            cancellationToken);

        var bytes    = _excel.ExportCheckoutHistory(rows);
        var filename = $"kit-checkout-history-{DateTime.UtcNow:yyyyMMdd}.xlsx";
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            filename);
    }

    [HttpGet("usage-summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsageSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken     cancellationToken)
    {
        var rows = await _mediator.Send(
            new GetKitUsageSummaryQuery(_tenantContext.TenantId, from, to),
            cancellationToken);

        return Ok(rows);
    }

    [HttpGet("usage-summary/export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportUsageSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken     cancellationToken)
    {
        var rows = await _mediator.Send(
            new GetKitUsageSummaryQuery(_tenantContext.TenantId, from, to),
            cancellationToken);

        var bytes    = _excel.ExportUsageSummary(rows);
        var filename = $"kit-usage-summary-{DateTime.UtcNow:yyyyMMdd}.xlsx";
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            filename);
    }
}
