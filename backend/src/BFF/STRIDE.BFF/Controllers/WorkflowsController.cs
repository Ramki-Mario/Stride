using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using STRIDE.BFF.HttpClients;

namespace STRIDE.BFF.Controllers;

/// <summary>
/// BFF proxy for Workflows module endpoints.
///
/// Angular calls /bff/workflows/* — this controller forwards with the session
/// JWT so STRIDE.Host can authenticate and apply tenant isolation.
///
/// Definitions:
///   GET    /bff/workflows/definitions
///   GET    /bff/workflows/definitions/{id}
///   POST   /bff/workflows/definitions
///   PUT    /bff/workflows/definitions/{id}
///   DELETE /bff/workflows/definitions/{id}
///   POST   /bff/workflows/definitions/{id}/activate
///   POST   /bff/workflows/definitions/{id}/start
///   GET    /bff/workflows/definitions/{id}/instances
///
/// Instances:
///   GET    /bff/workflows/instances/{instanceId}
///   POST   /bff/workflows/instances/{instanceId}/pause
///   POST   /bff/workflows/instances/{instanceId}/resume
///   POST   /bff/workflows/instances/{instanceId}/cancel
///
/// Steps:
///   POST   /bff/workflows/instances/{instanceId}/steps/{stepId}/assign
///   POST   /bff/workflows/instances/{instanceId}/steps/{stepId}/complete
///   POST   /bff/workflows/instances/{instanceId}/steps/{stepId}/fail
///   POST   /bff/workflows/instances/{instanceId}/steps/{stepId}/skip
/// </summary>
[ApiController]
[Authorize]
[Route("bff/workflows")]
public sealed class WorkflowsController : ControllerBase
{
    private readonly WorkflowApiClient            _workflows;
    private readonly ILogger<WorkflowsController> _logger;

    public WorkflowsController(
        WorkflowApiClient workflows,
        ILogger<WorkflowsController> logger)
    {
        _workflows = workflows;
        _logger    = logger;
    }

    // ── Definitions ───────────────────────────────────────────────────────

    [HttpGet("definitions")]
    public async Task<IActionResult> GetDefinitions(CancellationToken ct)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(await _workflows.GetDefinitionsAsync(token, ct));
    }

    [HttpGet("definitions/{id:guid}")]
    public async Task<IActionResult> GetDefinition(Guid id, CancellationToken ct)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(await _workflows.GetDefinitionAsync(id, token, ct));
    }

    [HttpPost("definitions")]
    public async Task<IActionResult> CreateDefinition(CancellationToken ct)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        using var body = JsonBody();
        return await ProxyAsync(await _workflows.CreateDefinitionAsync(body, token, ct), forwardStatusCode: true);
    }

    [HttpPut("definitions/{id:guid}")]
    public async Task<IActionResult> UpdateDefinition(Guid id, CancellationToken ct)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        using var body = JsonBody();
        return await ProxyAsync(await _workflows.UpdateDefinitionAsync(id, body, token, ct));
    }

    [HttpDelete("definitions/{id:guid}")]
    public async Task<IActionResult> DeleteDefinition(Guid id, CancellationToken ct)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _workflows.DeleteDefinitionAsync(id, token, ct);
        return response.IsSuccessStatusCode ? NoContent() : StatusCode((int)response.StatusCode);
    }

    [HttpPost("definitions/{id:guid}/activate")]
    public async Task<IActionResult> ActivateDefinition(Guid id, CancellationToken ct)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _workflows.ActivateDefinitionAsync(id, token, ct);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response);
    }

    [HttpPost("definitions/{id:guid}/start")]
    public async Task<IActionResult> StartInstance(Guid id, CancellationToken ct)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(await _workflows.StartInstanceAsync(id, token, ct), forwardStatusCode: true);
    }

    [HttpGet("definitions/{id:guid}/instances")]
    public async Task<IActionResult> GetInstancesByDefinition(Guid id, CancellationToken ct)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(await _workflows.GetInstancesByDefinitionAsync(id, token, ct));
    }

    // ── Instances ─────────────────────────────────────────────────────────

    [HttpGet("instances/{instanceId:guid}")]
    public async Task<IActionResult> GetInstance(Guid instanceId, CancellationToken ct)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        return await ProxyAsync(await _workflows.GetInstanceAsync(instanceId, token, ct));
    }

    [HttpPost("instances/{instanceId:guid}/pause")]
    public async Task<IActionResult> PauseInstance(Guid instanceId, CancellationToken ct)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _workflows.PauseInstanceAsync(instanceId, token, ct);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response);
    }

    [HttpPost("instances/{instanceId:guid}/resume")]
    public async Task<IActionResult> ResumeInstance(Guid instanceId, CancellationToken ct)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _workflows.ResumeInstanceAsync(instanceId, token, ct);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response);
    }

    [HttpPost("instances/{instanceId:guid}/cancel")]
    public async Task<IActionResult> CancelInstance(Guid instanceId, CancellationToken ct)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _workflows.CancelInstanceAsync(instanceId, token, ct);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response);
    }

    // ── Steps ─────────────────────────────────────────────────────────────

    [HttpPost("instances/{instanceId:guid}/steps/{stepId:guid}/assign")]
    public async Task<IActionResult> AssignStep(Guid instanceId, Guid stepId, CancellationToken ct)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        using var body = JsonBody();
        var response = await _workflows.AssignStepAsync(instanceId, stepId, body, token, ct);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response);
    }

    [HttpPost("instances/{instanceId:guid}/steps/{stepId:guid}/complete")]
    public async Task<IActionResult> CompleteStep(Guid instanceId, Guid stepId, CancellationToken ct)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _workflows.CompleteStepAsync(instanceId, stepId, token, ct);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response);
    }

    [HttpPost("instances/{instanceId:guid}/steps/{stepId:guid}/fail")]
    public async Task<IActionResult> FailStep(Guid instanceId, Guid stepId, CancellationToken ct)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        using var body = JsonBody();
        var response = await _workflows.FailStepAsync(instanceId, stepId, body, token, ct);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response);
    }

    [HttpPost("instances/{instanceId:guid}/steps/{stepId:guid}/skip")]
    public async Task<IActionResult> SkipStep(Guid instanceId, Guid stepId, CancellationToken ct)
    {
        var token = await GetTokenAsync();
        if (token is null) return Unauthorized();
        var response = await _workflows.SkipStepAsync(instanceId, stepId, token, ct);
        return response.IsSuccessStatusCode ? NoContent() : await ProxyAsync(response);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private Task<string?> GetTokenAsync() => HttpContext.GetTokenAsync("access_token");

    /// <summary>Forwards the request body as application/json to the Host.</summary>
    private StreamContent JsonBody()
    {
        var body = new StreamContent(Request.Body);
        body.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        return body;
    }

    private async Task<IActionResult> ProxyAsync(
        HttpResponseMessage response,
        bool forwardStatusCode = false)
    {
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Host workflows API returned {StatusCode}: {Body}",
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
            StatusCode  = forwardStatusCode ? (int)response.StatusCode : StatusCodes.Status200OK,
        };
    }
}
