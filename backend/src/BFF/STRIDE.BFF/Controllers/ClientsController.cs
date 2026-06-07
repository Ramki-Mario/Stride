using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BFF.HttpClients;

namespace STRIDE.BFF.Controllers;

/// <summary>
/// BFF proxy for Clients module endpoints.
///
///   GET  /bff/clients                  — paged client list
///   POST /bff/clients                  — create client
///   GET  /bff/clients/{id}             — client detail
///   PUT  /bff/clients/{id}             — update client
///   PUT  /bff/clients/{id}/deactivate  — deactivate client
///   PUT  /bff/clients/{id}/reactivate  — reactivate client
/// </summary>
[ApiController]
[Authorize]
[Route("bff/clients")]
public sealed class ClientsController : ControllerBase
{
    private readonly ClientsApiClient           _clients;
    private readonly ILogger<ClientsController> _logger;

    public ClientsController(ClientsApiClient clients, ILogger<ClientsController> logger)
    {
        _clients = clients;
        _logger  = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetClients(
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 20,
        [FromQuery] string? search   = null,
        [FromQuery] int?    status   = null,
        CancellationToken cancellationToken = default)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(
            await _clients.GetClientsAsync(token, page, pageSize, search, status, cancellationToken), cancellationToken);
    }

    [HttpPost]
    public async Task<IActionResult> CreateClient([FromBody] object body, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _clients.CreateClientAsync(body, token, cancellationToken);
        return response.IsSuccessStatusCode
            ? StatusCode(StatusCodes.Status201Created, await response.Content.ReadAsStringAsync(cancellationToken))
            : await ProxyAsync(response, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetClient(Guid id, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(await _clients.GetClientByIdAsync(id, token, cancellationToken), cancellationToken);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateClient(
        Guid id, [FromBody] object body, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _clients.UpdateClientAsync(id, body, token, cancellationToken);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response, cancellationToken);
    }

    [HttpPut("{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateClient(Guid id, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _clients.DeactivateClientAsync(id, token, cancellationToken);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response, cancellationToken);
    }

    [HttpPut("{id:guid}/reactivate")]
    public async Task<IActionResult> ReactivateClient(Guid id, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _clients.ReactivateClientAsync(id, token, cancellationToken);
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
                "Host clients API returned {StatusCode}: {Body}",
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
