using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BFF.Extensions;
using STRIDE.BFF.HttpClients;

namespace STRIDE.BFF.Controllers;

/// <summary>
/// BFF proxy for Teams module endpoints.
///
///   GET  /bff/teams                  — paged team list
///   POST /bff/teams                  — create team
///   GET  /bff/teams/{id}             — team detail
///   PUT  /bff/teams/{id}             — update team
///   PUT  /bff/teams/{id}/deactivate  — deactivate team
///   PUT  /bff/teams/{id}/reactivate  — reactivate team
/// </summary>
[ApiController]
[Authorize]
[Route("bff/teams")]
public sealed class TeamsController : ControllerBase
{
    private readonly TeamsApiClient           _teams;
    private readonly ILogger<TeamsController> _logger;

    public TeamsController(TeamsApiClient teams, ILogger<TeamsController> logger)
    {
        _teams  = teams;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetTeams(
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 20,
        [FromQuery] string? search   = null,
        [FromQuery] int?    status   = null,
        CancellationToken cancellationToken = default)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(
            await _teams.GetTeamsAsync(token, page, pageSize, search, status, cancellationToken),
            cancellationToken);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTeam([FromBody] object body, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _teams.CreateTeamAsync(body, token, cancellationToken);
        return response.IsSuccessStatusCode
            ? StatusCode(StatusCodes.Status201Created, await response.Content.ReadAsStringAsync(cancellationToken))
            : await ProxyAsync(response, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetTeam(Guid id, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(await _teams.GetTeamByIdAsync(id, token, cancellationToken), cancellationToken);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateTeam(
        Guid id, [FromBody] object body, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _teams.UpdateTeamAsync(id, body, token, cancellationToken);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response, cancellationToken);
    }

    [HttpPut("{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateTeam(Guid id, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _teams.DeactivateTeamAsync(id, token, cancellationToken);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response, cancellationToken);
    }

    [HttpPut("{id:guid}/reactivate")]
    public async Task<IActionResult> ReactivateTeam(Guid id, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _teams.ReactivateTeamAsync(id, token, cancellationToken);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response, cancellationToken);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Task<string?> GetTokenAsync() => HttpContext.GetCurrentAccessTokenAsync();

    private async Task<IActionResult> ProxyAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Host teams API returned {StatusCode}: {Body}",
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
