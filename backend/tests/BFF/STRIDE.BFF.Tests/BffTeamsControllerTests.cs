using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.BFF.Controllers;
using STRIDE.BFF.HttpClients;

namespace STRIDE.BFF.Tests;

/// <summary>
/// Unit tests for the BFF <see cref="TeamsController"/>.
///
/// Because <see cref="TeamsApiClient"/> is a sealed concrete class, we pair it
/// with a <see cref="DelegatingHandlerStub"/> that returns controllable
/// <see cref="HttpResponseMessage"/> instances rather than mocking the client directly.
/// HttpContext is set up with a stub <see cref="IAuthenticationService"/> so that
/// <c>GetTokenAsync("access_token")</c> can return either a real token or null.
/// </summary>
public sealed class BffTeamsControllerTests
{
    private const string FakeToken = "test-bearer-token";

    // ── Factories ─────────────────────────────────────────────────────────────

    private static (TeamsController sut, Action<HttpResponseMessage> setResponse) BuildSut(
        string? token = FakeToken)
    {
        HttpResponseMessage? response = null;

        var handler = new DelegatingHandlerStub(_ =>
            response ?? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });

        var client     = new TeamsApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://host/") });
        var logger     = Substitute.For<ILogger<TeamsController>>();
        var controller = new TeamsController(client, logger);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = BuildHttpContext(token),
        };

        return (controller, r => response = r);
    }

    private static DefaultHttpContext BuildHttpContext(string? token)
    {
        var authService = Substitute.For<IAuthenticationService>();
        var properties  = token is null
            ? new AuthenticationProperties()
            : new AuthenticationProperties(new Dictionary<string, string?> { [".Token.access_token"] = token });

        authService.AuthenticateAsync(Arg.Any<HttpContext>(), Arg.Any<string?>())
            .Returns(token is null
                ? AuthenticateResult.NoResult()
                : AuthenticateResult.Success(
                    new AuthenticationTicket(new ClaimsPrincipal(), properties, "test")));

        var services = new ServiceCollection();
        services.AddSingleton(authService);

        return new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
    }

    // ── Unauthorized paths (null token) ──────────────────────────────────────

    [Fact]
    public async Task GetTeams_NullToken_ReturnsUnauthorized()
    {
        var (sut, _) = BuildSut(token: null);

        var result = await sut.GetTeams();

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task CreateTeam_NullToken_ReturnsUnauthorized()
    {
        var (sut, _) = BuildSut(token: null);

        var result = await sut.CreateTeam(new { }, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task GetTeam_NullToken_ReturnsUnauthorized()
    {
        var (sut, _) = BuildSut(token: null);

        var result = await sut.GetTeam(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    // ── Success paths ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTeams_ValidToken_ProxiesSuccessResponse()
    {
        var (sut, _) = BuildSut();

        var result = await sut.GetTeams();

        result.Should().BeOfType<ContentResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task CreateTeam_ValidToken_Returns201()
    {
        var (sut, _) = BuildSut();

        var result = await sut.CreateTeam(new { Name = "Test" }, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    [Fact]
    public async Task GetTeam_ValidToken_ProxiesSuccessResponse()
    {
        var (sut, _) = BuildSut();

        var result = await sut.GetTeam(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<ContentResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task UpdateTeam_ValidToken_Returns204()
    {
        var (sut, _) = BuildSut();

        var result = await sut.UpdateTeam(Guid.NewGuid(), new { }, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeactivateTeam_ValidToken_Returns204()
    {
        var (sut, _) = BuildSut();

        var result = await sut.DeactivateTeam(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task ReactivateTeam_ValidToken_Returns204()
    {
        var (sut, _) = BuildSut();

        var result = await sut.ReactivateTeam(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    // ── ProxyAsync error path ─────────────────────────────────────────────────

    [Fact]
    public async Task GetTeams_UpstreamError_ProxiesErrorStatusCode()
    {
        var (sut, setResponse) = BuildSut();
        setResponse(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent("{\"error\":\"downstream unavailable\"}"),
        });

        var result = await sut.GetTeams();

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task CreateTeam_UpstreamError_ProxiesErrorResponse()
    {
        var (sut, setResponse) = BuildSut();
        setResponse(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"error\":\"validation failed\"}"),
        });

        var result = await sut.CreateTeam(new { }, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

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
