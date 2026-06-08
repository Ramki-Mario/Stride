using System.Net.Http.Headers;

namespace STRIDE.BFF.HttpClients;

/// <summary>
/// Typed HttpClient for the Workflows module surface of STRIDE.Host (ADR-011).
///
/// Extracts the JWT from the Redis session and forwards it as a Bearer token so
/// the Host can authenticate and apply tenant isolation on every workflow operation.
///
/// BFF route → Host route mapping:
///   GET    /bff/workflows/definitions                              → GET    /api/workflows
///   GET    /bff/workflows/definitions/{id}                        → GET    /api/workflows/{id}
///   POST   /bff/workflows/definitions                             → POST   /api/workflows
///   PUT    /bff/workflows/definitions/{id}                        → PUT    /api/workflows/{id}
///   DELETE /bff/workflows/definitions/{id}                        → DELETE /api/workflows/{id}
///   POST   /bff/workflows/definitions/{id}/activate               → POST   /api/workflows/{id}/activate
///   POST   /bff/workflows/definitions/{id}/start                  → POST   /api/workflows/{id}/start
///   GET    /bff/workflows/definitions/{id}/instances              → GET    /api/workflows/{id}/instances
///   GET    /bff/workflows/instances/{instanceId}                  → GET    /api/workflows/instances/{instanceId}
///   POST   /bff/workflows/instances/{instanceId}/pause            → POST   /api/workflows/instances/{instanceId}/pause
///   POST   /bff/workflows/instances/{instanceId}/resume           → POST   /api/workflows/instances/{instanceId}/resume
///   POST   /bff/workflows/instances/{instanceId}/cancel           → POST   /api/workflows/instances/{instanceId}/cancel
///   POST   /bff/workflows/instances/{iId}/steps/{sId}/assign      → POST   /api/workflows/instances/{iId}/steps/{sId}/assign
///   POST   /bff/workflows/instances/{iId}/steps/{sId}/complete    → POST   /api/workflows/instances/{iId}/steps/{sId}/complete
///   POST   /bff/workflows/instances/{iId}/steps/{sId}/fail        → POST   /api/workflows/instances/{iId}/steps/{sId}/fail
///   POST   /bff/workflows/instances/{iId}/steps/{sId}/skip        → POST   /api/workflows/instances/{iId}/steps/{sId}/skip
///   GET    /bff/workflows/my-tasks                                → GET    /api/workflows/my-tasks
/// </summary>
public sealed class WorkflowApiClient
{
    private const string EmptyJson   = "{}";
    private const string AppJson     = "application/json";

    private readonly HttpClient _client;

    public WorkflowApiClient(HttpClient client) => _client = client;

    // ── Definitions ───────────────────────────────────────────────────────

    public Task<HttpResponseMessage> GetDefinitionsAsync(string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(Build(HttpMethod.Get, "/api/workflows", token), cancellationToken);

    public Task<HttpResponseMessage> GetDefinitionAsync(Guid id, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(Build(HttpMethod.Get, $"/api/workflows/{id}", token), cancellationToken);

    public Task<HttpResponseMessage> CreateDefinitionAsync(HttpContent body, string token, CancellationToken cancellationToken = default)
    {
        var req = Build(HttpMethod.Post, "/api/workflows", token);
        req.Content = body;
        return _client.SendAsync(req, cancellationToken);
    }

    public Task<HttpResponseMessage> UpdateDefinitionAsync(Guid id, HttpContent body, string token, CancellationToken cancellationToken = default)
    {
        var req = Build(HttpMethod.Put, $"/api/workflows/{id}", token);
        req.Content = body;
        return _client.SendAsync(req, cancellationToken);
    }

    public Task<HttpResponseMessage> DeleteDefinitionAsync(Guid id, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(Build(HttpMethod.Delete, $"/api/workflows/{id}", token), cancellationToken);

    public Task<HttpResponseMessage> ActivateDefinitionAsync(Guid id, string token, CancellationToken cancellationToken = default)
    {
        var req = Build(HttpMethod.Post, $"/api/workflows/{id}/activate", token);
        req.Content = new StringContent(EmptyJson, System.Text.Encoding.UTF8, AppJson);
        return _client.SendAsync(req, cancellationToken);
    }

    public Task<HttpResponseMessage> StartInstanceAsync(Guid definitionId, string token, CancellationToken cancellationToken = default)
    {
        var req = Build(HttpMethod.Post, $"/api/workflows/{definitionId}/start", token);
        req.Content = new StringContent(EmptyJson, System.Text.Encoding.UTF8, AppJson);
        return _client.SendAsync(req, cancellationToken);
    }

    public Task<HttpResponseMessage> GetInstancesByDefinitionAsync(Guid definitionId, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(Build(HttpMethod.Get, $"/api/workflows/{definitionId}/instances", token), cancellationToken);

    // ── Instances ─────────────────────────────────────────────────────────

    public Task<HttpResponseMessage> GetMyTasksAsync(string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(Build(HttpMethod.Get, "/api/workflows/my-tasks", token), cancellationToken);

    public Task<HttpResponseMessage> GetAllInstancesAsync(string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(Build(HttpMethod.Get, "/api/workflows/instances", token), cancellationToken);

    public Task<HttpResponseMessage> GetInstanceAsync(Guid instanceId, string token, CancellationToken cancellationToken = default)
        => _client.SendAsync(Build(HttpMethod.Get, $"/api/workflows/instances/{instanceId}", token), cancellationToken);

    public Task<HttpResponseMessage> PauseInstanceAsync(Guid instanceId, string token, CancellationToken cancellationToken = default)
    {
        var req = Build(HttpMethod.Post, $"/api/workflows/instances/{instanceId}/pause", token);
        req.Content = new StringContent(EmptyJson, System.Text.Encoding.UTF8, AppJson);
        return _client.SendAsync(req, cancellationToken);
    }

    public Task<HttpResponseMessage> ResumeInstanceAsync(Guid instanceId, string token, CancellationToken cancellationToken = default)
    {
        var req = Build(HttpMethod.Post, $"/api/workflows/instances/{instanceId}/resume", token);
        req.Content = new StringContent(EmptyJson, System.Text.Encoding.UTF8, AppJson);
        return _client.SendAsync(req, cancellationToken);
    }

    public Task<HttpResponseMessage> CancelInstanceAsync(Guid instanceId, string token, CancellationToken cancellationToken = default)
    {
        var req = Build(HttpMethod.Post, $"/api/workflows/instances/{instanceId}/cancel", token);
        req.Content = new StringContent(EmptyJson, System.Text.Encoding.UTF8, AppJson);
        return _client.SendAsync(req, cancellationToken);
    }

    // ── Steps ─────────────────────────────────────────────────────────────

    public Task<HttpResponseMessage> AssignStepAsync(Guid instanceId, Guid stepId, HttpContent body, string token, CancellationToken cancellationToken = default)
    {
        var req = Build(HttpMethod.Post, $"/api/workflows/instances/{instanceId}/steps/{stepId}/assign", token);
        req.Content = body;
        return _client.SendAsync(req, cancellationToken);
    }

    public Task<HttpResponseMessage> CompleteStepAsync(Guid instanceId, Guid stepId, HttpContent? body, string token, CancellationToken cancellationToken = default)
    {
        var req = Build(HttpMethod.Post, $"/api/workflows/instances/{instanceId}/steps/{stepId}/complete", token);
        req.Content = body ?? new StringContent(EmptyJson, System.Text.Encoding.UTF8, AppJson);
        return _client.SendAsync(req, cancellationToken);
    }

    public Task<HttpResponseMessage> FailStepAsync(Guid instanceId, Guid stepId, HttpContent body, string token, CancellationToken cancellationToken = default)
    {
        var req = Build(HttpMethod.Post, $"/api/workflows/instances/{instanceId}/steps/{stepId}/fail", token);
        req.Content = body;
        return _client.SendAsync(req, cancellationToken);
    }

    public Task<HttpResponseMessage> SkipStepAsync(Guid instanceId, Guid stepId, string token, CancellationToken cancellationToken = default)
    {
        var req = Build(HttpMethod.Post, $"/api/workflows/instances/{instanceId}/steps/{stepId}/skip", token);
        req.Content = new StringContent(EmptyJson, System.Text.Encoding.UTF8, AppJson);
        return _client.SendAsync(req, cancellationToken);
    }

    public Task<HttpResponseMessage> ClaimStepAsync(Guid instanceId, Guid stepId, string token, CancellationToken cancellationToken = default)
    {
        var req = Build(HttpMethod.Post, $"/api/workflows/instances/{instanceId}/steps/{stepId}/claim", token);
        req.Content = new StringContent(EmptyJson, System.Text.Encoding.UTF8, AppJson);
        return _client.SendAsync(req, cancellationToken);
    }

    // ── Step Attachments ──────────────────────────────────────────────────

    /// <summary>
    /// Forwards a multipart/form-data upload to the Host.
    /// The caller is responsible for setting the correct Content-Type (including boundary).
    /// </summary>
    public Task<HttpResponseMessage> UploadStepAttachmentAsync(
        Guid        instanceId,
        Guid        stepId,
        HttpContent multipartContent,
        string      token,
        CancellationToken cancellationToken = default)
    {
        var req = Build(HttpMethod.Post,
            $"/api/workflows/instances/{instanceId}/steps/{stepId}/attachments", token);
        req.Content = multipartContent;
        return _client.SendAsync(req, cancellationToken);
    }

    public Task<HttpResponseMessage> ListStepAttachmentsAsync(
        Guid instanceId,
        Guid stepId,
        string token,
        CancellationToken cancellationToken = default)
        => _client.SendAsync(Build(HttpMethod.Get,
            $"/api/workflows/instances/{instanceId}/steps/{stepId}/attachments", token),
            cancellationToken);

    /// <summary>
    /// Downloads a file. The response carries binary content — callers must NOT
    /// read it as JSON; stream it directly to the HTTP response body.
    /// </summary>
    public Task<HttpResponseMessage> DownloadStepAttachmentAsync(
        Guid instanceId,
        Guid stepId,
        Guid attachmentId,
        string token,
        CancellationToken cancellationToken = default)
        => _client.SendAsync(Build(HttpMethod.Get,
            $"/api/workflows/instances/{instanceId}/steps/{stepId}/attachments/{attachmentId}/download",
            token), HttpCompletionOption.ResponseHeadersRead, cancellationToken);

    public Task<HttpResponseMessage> DeleteStepAttachmentAsync(
        Guid instanceId,
        Guid stepId,
        Guid attachmentId,
        string token,
        CancellationToken cancellationToken = default)
        => _client.SendAsync(Build(HttpMethod.Delete,
            $"/api/workflows/instances/{instanceId}/steps/{stepId}/attachments/{attachmentId}",
            token), cancellationToken);

    // ── Instance Attachments ──────────────────────────────────────────────

    /// <summary>
    /// Forwards a multipart/form-data upload to the Host (instance-level — no step).
    /// The caller is responsible for setting the correct Content-Type (including boundary).
    /// </summary>
    public Task<HttpResponseMessage> UploadInstanceAttachmentAsync(
        Guid        instanceId,
        HttpContent multipartContent,
        string      token,
        CancellationToken cancellationToken = default)
    {
        var req = Build(HttpMethod.Post,
            $"/api/workflows/instances/{instanceId}/attachments", token);
        req.Content = multipartContent;
        return _client.SendAsync(req, cancellationToken);
    }

    /// <summary>
    /// Lists all attachments (step-level + instance-level) for a workflow instance.
    /// </summary>
    public Task<HttpResponseMessage> ListWorkflowAttachmentsAsync(
        Guid instanceId,
        string token,
        CancellationToken cancellationToken = default)
        => _client.SendAsync(Build(HttpMethod.Get,
            $"/api/workflows/instances/{instanceId}/attachments", token),
            cancellationToken);

    /// <summary>
    /// Downloads an instance-level attachment file.
    /// Uses ResponseHeadersRead so the response stream is not buffered.
    /// </summary>
    public Task<HttpResponseMessage> DownloadInstanceAttachmentAsync(
        Guid instanceId,
        Guid attachmentId,
        string token,
        CancellationToken cancellationToken = default)
        => _client.SendAsync(Build(HttpMethod.Get,
            $"/api/workflows/instances/{instanceId}/attachments/{attachmentId}/download",
            token), HttpCompletionOption.ResponseHeadersRead, cancellationToken);

    public Task<HttpResponseMessage> DeleteInstanceAttachmentAsync(
        Guid instanceId,
        Guid attachmentId,
        string token,
        CancellationToken cancellationToken = default)
        => _client.SendAsync(Build(HttpMethod.Delete,
            $"/api/workflows/instances/{instanceId}/attachments/{attachmentId}",
            token), cancellationToken);

    // ── Helper ────────────────────────────────────────────────────────────

    private static HttpRequestMessage Build(HttpMethod method, string uri, string token)
    {
        var req = new HttpRequestMessage(method, uri);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return req;
    }
}
