using System.Net.Http.Headers;
using STRIDE.BFF.HttpClients;

namespace STRIDE.BFF.Tests;

/// <summary>
/// Unit tests for <see cref="TeamsApiClient"/>.
/// A custom <see cref="HttpMessageHandler"/> captures outgoing requests and
/// returns configurable responses — no real HTTP traffic required.
/// </summary>
public sealed class TeamsApiClientTests
{
    private HttpRequestMessage?    _capturedRequest;
    private HttpResponseMessage    _stubbedResponse = new(HttpStatusCode.OK) { Content = new StringContent("{}") };

    private TeamsApiClient BuildSut()
    {
        var handler = new DelegatingHandlerStub(req =>
        {
            _capturedRequest = req;
            return _stubbedResponse;
        });
        return new TeamsApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://host/") });
    }

    // ── GetTeamsAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTeamsAsync_BasicParams_UsesGetAndCorrectUrl()
    {
        await BuildSut().GetTeamsAsync("tok", 1, 20, null, null);

        _capturedRequest!.Method.Should().Be(HttpMethod.Get);
        _capturedRequest.RequestUri!.PathAndQuery.Should().Contain("/api/teams")
            .And.Contain("page=1")
            .And.Contain("pageSize=20");
    }

    [Fact]
    public async Task GetTeamsAsync_WithSearch_IncludesSearchInQueryString()
    {
        await BuildSut().GetTeamsAsync("tok", 1, 20, "engineering", null);

        _capturedRequest!.RequestUri!.Query.Should().Contain("search=engineering");
    }

    [Fact]
    public async Task GetTeamsAsync_WithStatus_IncludesStatusInQueryString()
    {
        await BuildSut().GetTeamsAsync("tok", 1, 20, null, 0);

        _capturedRequest!.RequestUri!.Query.Should().Contain("status=0");
    }

    [Fact]
    public async Task GetTeamsAsync_SetsBearerToken()
    {
        await BuildSut().GetTeamsAsync("my-token", 1, 20, null, null);

        _capturedRequest!.Headers.Authorization.Should().BeEquivalentTo(
            new AuthenticationHeaderValue("Bearer", "my-token"));
    }

    // ── GetTeamByIdAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetTeamByIdAsync_UsesGetAndIncludesId()
    {
        var id = Guid.NewGuid();

        await BuildSut().GetTeamByIdAsync(id, "tok");

        _capturedRequest!.Method.Should().Be(HttpMethod.Get);
        _capturedRequest.RequestUri!.PathAndQuery.Should().Contain(id.ToString());
    }

    // ── CreateTeamAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateTeamAsync_UsesPostWithJsonBody()
    {
        var body = new { Name = "Engineering" };

        await BuildSut().CreateTeamAsync(body, "tok");

        _capturedRequest!.Method.Should().Be(HttpMethod.Post);
        _capturedRequest.RequestUri!.PathAndQuery.Should().Contain("/api/teams");
        var content = await _capturedRequest.Content!.ReadAsStringAsync();
        content.Should().Contain("Engineering");
    }

    // ── UpdateTeamAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateTeamAsync_UsesPutWithIdInUrlAndJsonBody()
    {
        var id   = Guid.NewGuid();
        var body = new { Name = "Backend" };

        await BuildSut().UpdateTeamAsync(id, body, "tok");

        _capturedRequest!.Method.Should().Be(HttpMethod.Put);
        _capturedRequest.RequestUri!.PathAndQuery.Should().Contain(id.ToString());
        var content = await _capturedRequest.Content!.ReadAsStringAsync();
        content.Should().Contain("Backend");
    }

    // ── DeactivateTeamAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateTeamAsync_UsesPutWithIdInUrlAndEmptyBody()
    {
        var id = Guid.NewGuid();

        await BuildSut().DeactivateTeamAsync(id, "tok");

        _capturedRequest!.Method.Should().Be(HttpMethod.Put);
        _capturedRequest.RequestUri!.PathAndQuery.Should().Contain($"{id}/deactivate");
        var content = await _capturedRequest.Content!.ReadAsStringAsync();
        content.Should().Be("{}");
    }

    // ── ReactivateTeamAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task ReactivateTeamAsync_UsesPutWithIdInUrlAndEmptyBody()
    {
        var id = Guid.NewGuid();

        await BuildSut().ReactivateTeamAsync(id, "tok");

        _capturedRequest!.Method.Should().Be(HttpMethod.Put);
        _capturedRequest.RequestUri!.PathAndQuery.Should().Contain($"{id}/reactivate");
        var content = await _capturedRequest.Content!.ReadAsStringAsync();
        content.Should().Be("{}");
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private sealed class DelegatingHandlerStub : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public DelegatingHandlerStub(Func<HttpRequestMessage, HttpResponseMessage> handler)
            => _handler = handler;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_handler(request));
    }
}
