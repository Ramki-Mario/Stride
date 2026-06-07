using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BFF.HttpClients;

namespace STRIDE.BFF.Controllers;

/// <summary>
/// BFF proxy for Invoicing module endpoints.
///
///   GET    /bff/invoicing/invoices                                    — paged invoice list
///   POST   /bff/invoicing/invoices                                    — generate invoice
///   GET    /bff/invoicing/invoices/{id}                               — invoice detail
///   GET    /bff/invoicing/invoices/by-workflow/{workflowInstanceId}   — invoice for workflow instance
///   POST   /bff/invoicing/invoices/from-workflow/{workflowInstanceId} — create draft from workflow
///   PUT    /bff/invoicing/invoices/{id}/send                          — send invoice
///   PUT    /bff/invoicing/invoices/{id}/paid                          — mark paid
///   PUT    /bff/invoicing/invoices/{id}/void                          — void invoice
/// </summary>
[ApiController]
[Authorize]
[Route("bff/invoicing")]
public sealed class InvoicingController : ControllerBase
{
    private readonly InvoicingApiClient           _invoicing;
    private readonly ILogger<InvoicingController> _logger;

    public InvoicingController(InvoicingApiClient invoicing, ILogger<InvoicingController> logger)
    {
        _invoicing = invoicing;
        _logger    = logger;
    }

    [HttpGet("invoices")]
    public async Task<IActionResult> GetInvoices(
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 20,
        [FromQuery] string? search   = null,
        [FromQuery] int?    status   = null,
        CancellationToken cancellationToken = default)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(
            await _invoicing.GetInvoicesAsync(token, page, pageSize, search, status, cancellationToken), cancellationToken);
    }

    [HttpPost("invoices")]
    public async Task<IActionResult> GenerateInvoice([FromBody] object body, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _invoicing.GenerateInvoiceAsync(body, token, cancellationToken);
        return response.IsSuccessStatusCode
            ? StatusCode(StatusCodes.Status201Created, await response.Content.ReadAsStringAsync(cancellationToken))
            : await ProxyAsync(response, cancellationToken);
    }

    [HttpGet("invoices/{id:guid}")]
    public async Task<IActionResult> GetInvoice(Guid id, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(await _invoicing.GetInvoiceByIdAsync(id, token, cancellationToken), cancellationToken);
    }

    [HttpGet("invoices/by-workflow/{workflowInstanceId:guid}")]
    public async Task<IActionResult> GetInvoiceByWorkflowInstance(
        Guid workflowInstanceId, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(
            await _invoicing.GetInvoiceByWorkflowInstanceIdAsync(workflowInstanceId, token, cancellationToken),
            cancellationToken);
    }

    [HttpPost("invoices/from-workflow/{workflowInstanceId:guid}")]
    public async Task<IActionResult> CreateInvoiceFromWorkflow(
        Guid workflowInstanceId,
        [FromBody] object body,
        CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(
            await _invoicing.CreateInvoiceFromWorkflowAsync(workflowInstanceId, body, token, cancellationToken),
            cancellationToken);
    }

    [HttpPut("invoices/{id:guid}/send")]
    public async Task<IActionResult> SendInvoice(Guid id, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _invoicing.SendInvoiceAsync(id, token, cancellationToken);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response, cancellationToken);
    }

    [HttpPut("invoices/{id:guid}/paid")]
    public async Task<IActionResult> MarkPaid(Guid id, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _invoicing.MarkPaidAsync(id, token, cancellationToken);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response, cancellationToken);
    }

    [HttpPut("invoices/{id:guid}/void")]
    public async Task<IActionResult> VoidInvoice(Guid id, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _invoicing.VoidInvoiceAsync(id, token, cancellationToken);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response, cancellationToken);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Task<string?> GetTokenAsync() => HttpContext.GetTokenAsync("access_token");

    private async Task<IActionResult> ProxyAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Host invoicing API returned {StatusCode}: {Body}",
                (int)response.StatusCode, json);

            return StatusCode((int)response.StatusCode,
                new ProblemDetails
                {
                    Title  = "Upstream error",
                    Detail = json,
                    Status = (int)response.StatusCode,
                });
        }

        return new ContentResult
        {
            Content     = json,
            ContentType = "application/json",
            StatusCode  = StatusCodes.Status200OK,
        };
    }
}
