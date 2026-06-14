using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using STRIDE.BFF.Auth;
using STRIDE.BFF.Controllers;
using STRIDE.BFF.HttpClients;

namespace STRIDE.BFF.Tests;

/// <summary>
/// Unit tests for the BFF <see cref="AuthController"/> refresh and logout endpoints.
///
/// <see cref="IdentityApiClient"/> is driven via <see cref="DelegatingHandlerStub"/> to
/// control upstream HTTP responses. <see cref="IAuthenticationService"/> is NSubstituted
/// so that <c>GetTokenAsync("refresh_token")</c>, <c>SignInAsync</c>, and <c>SignOutAsync</c>
/// work without a real ASP.NET pipeline.
/// </summary>
public sealed class BffAuthControllerTests
{
    private static readonly string FakeRefreshToken = new('r', 64);
    private const string FakeAccessToken  = "new_access_token_value";

    private static readonly DateTime FakeAccessExpiry  = DateTime.UtcNow.AddHours(8);
    private static readonly DateTime FakeRefreshExpiry = DateTime.UtcNow.AddDays(7);

    // ── POST /bff/auth/refresh ────────────────────────────────────────────────

    [Fact]
    public async Task Refresh_WhenNoRefreshTokenInSession_ReturnsUnauthorized()
    {
        var (sut, _) = BuildSut(refreshToken: null, upstreamResponse: null);

        var result = await sut.Refresh(CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Refresh_WhenHostReturns401_ReturnsUnauthorized()
    {
        var (sut, _) = BuildSut(
            refreshToken: FakeRefreshToken,
            upstreamResponse: new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var result = await sut.Refresh(CancellationToken.None);

        result.Should().BeOfType<UnauthorizedResult>();
    }

    [Fact]
    public async Task Refresh_WhenHostSucceeds_ReturnsNoContent()
    {
        var tokenPair = new HostTokenPairResponse(
            FakeAccessToken, FakeAccessExpiry,
            FakeRefreshToken, FakeRefreshExpiry);

        var (sut, _) = BuildSut(
            refreshToken: FakeRefreshToken,
            upstreamResponse: BuildJsonResponse(tokenPair));

        var result = await sut.Refresh(CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Refresh_WhenHostSucceeds_CallsSignIn()
    {
        var tokenPair = new HostTokenPairResponse(
            FakeAccessToken, FakeAccessExpiry,
            FakeRefreshToken, FakeRefreshExpiry);

        var (sut, authService) = BuildSut(
            refreshToken: FakeRefreshToken,
            upstreamResponse: BuildJsonResponse(tokenPair));

        await sut.Refresh(CancellationToken.None);

        await authService.Received(1).SignInAsync(
            Arg.Any<HttpContext>(),
            CookieAuthenticationDefaults.AuthenticationScheme,
            Arg.Any<ClaimsPrincipal>(),
            Arg.Any<AuthenticationProperties>());
    }

    // ── POST /bff/auth/logout ─────────────────────────────────────────────────

    [Fact]
    public async Task Logout_WithRefreshTokenInSession_CallsHostRevoke()
    {
        HttpRequestMessage? captured = null;

        var (sut, _) = BuildSut(
            refreshToken: FakeRefreshToken,
            upstreamResponse: new HttpResponseMessage(HttpStatusCode.NoContent),
            captureRequest: r => captured = r);

        await sut.Logout(CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.RequestUri!.PathAndQuery.Should().Be("/api/identity/auth/revoke");
    }

    [Fact]
    public async Task Logout_WithRefreshTokenInSession_ReturnsNoContent()
    {
        var (sut, _) = BuildSut(
            refreshToken: FakeRefreshToken,
            upstreamResponse: new HttpResponseMessage(HttpStatusCode.NoContent));

        var result = await sut.Logout(CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Logout_WhenHostRevokeThrows_StillSignsOut()
    {
        var authService = Substitute.For<IAuthenticationService>();
        SetupAuthService(authService, FakeRefreshToken);

        var handler = new DelegatingHandlerStub(_ => throw new HttpRequestException("host down"));
        var identity = new IdentityApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://host/") });
        var logger   = Substitute.For<ILogger<AuthController>>();
        var tenant   = BuildTenantClient();

        var sut = new AuthController(identity, tenant, logger);
        sut.ControllerContext = new ControllerContext
        {
            HttpContext = BuildHttpContext(authService, FakeRefreshToken)
        };

        var result = await sut.Logout(CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        await authService.Received(1).SignOutAsync(
            Arg.Any<HttpContext>(),
            CookieAuthenticationDefaults.AuthenticationScheme,
            Arg.Any<AuthenticationProperties?>());
    }

    // ── Factories ─────────────────────────────────────────────────────────────

    private static (AuthController sut, IAuthenticationService authService) BuildSut(
        string? refreshToken,
        HttpResponseMessage? upstreamResponse,
        Action<HttpRequestMessage>? captureRequest = null)
    {
        var handler = new DelegatingHandlerStub(req =>
        {
            captureRequest?.Invoke(req);
            return upstreamResponse ?? new HttpResponseMessage(HttpStatusCode.OK);
        });

        var identity = new IdentityApiClient(new HttpClient(handler) { BaseAddress = new Uri("https://host/") });
        var logger   = Substitute.For<ILogger<AuthController>>();
        var tenant   = BuildTenantClient();

        var authService = Substitute.For<IAuthenticationService>();
        SetupAuthService(authService, refreshToken);

        var sut = new AuthController(identity, tenant, logger);
        sut.ControllerContext = new ControllerContext
        {
            HttpContext = BuildHttpContext(authService, refreshToken)
        };

        return (sut, authService);
    }

    private static void SetupAuthService(IAuthenticationService authService, string? refreshToken)
    {
        var properties = refreshToken is null
            ? new AuthenticationProperties()
            : new AuthenticationProperties(
                new Dictionary<string, string?> { [".Token.refresh_token"] = refreshToken });

        authService.AuthenticateAsync(Arg.Any<HttpContext>(), Arg.Any<string?>())
            .Returns(refreshToken is null
                ? AuthenticateResult.NoResult()
                : AuthenticateResult.Success(
                    new AuthenticationTicket(
                        new ClaimsPrincipal(new ClaimsIdentity(
                            new[] { new Claim("sub", Guid.NewGuid().ToString("N")) },
                            CookieAuthenticationDefaults.AuthenticationScheme)),
                        properties,
                        CookieAuthenticationDefaults.AuthenticationScheme)));
    }

    private static TenantSettingsApiClient BuildTenantClient()
    {
        var stub = new DelegatingHandlerStub(_ => new HttpResponseMessage(HttpStatusCode.OK));
        return new TenantSettingsApiClient(new HttpClient(stub) { BaseAddress = new Uri("https://host/") });
    }

    private static DefaultHttpContext BuildHttpContext(IAuthenticationService authService, string? refreshToken)
    {
        var services = new ServiceCollection();
        services.AddSingleton(authService);
        return new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
    }

    private static HttpResponseMessage BuildJsonResponse<T>(T value)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(value,
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
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
