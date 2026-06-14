using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using STRIDE.BFF.Auth;
using STRIDE.BFF.Extensions;
using STRIDE.BFF.HttpClients;
using STRIDE.BFF.Middleware;

namespace STRIDE.BFF.Tests;

/// <summary>
/// Unit tests for <see cref="SilentTokenRenewalMiddleware"/>.
///
/// The middleware receives <see cref="IdentityApiClient"/>, <see cref="TokenRenewalLockProvider"/>,
/// and <see cref="IOptions{SilentTokenRenewalOptions}"/> via DI at call time (convention-based
/// middleware injection). Tests drive the middleware by calling InvokeAsync directly.
/// </summary>
public sealed class SilentTokenRenewalMiddlewareTests
{
    private static readonly System.Text.Json.JsonSerializerOptions CamelCase =
        new() { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase };

    private const string FakeAccessToken  = "access_token_value";
    private const string FakeRefreshToken = "refresh_token_value";
    private static readonly DateTime FarFuture = DateTime.UtcNow.AddHours(8);
    private static readonly DateTime SoonExpiry = DateTime.UtcNow.AddMinutes(3); // within default 5-min skew
    private static readonly DateTime AlreadyExpired = DateTime.UtcNow.AddMinutes(-1);

    // ── Skip conditions ───────────────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_ForAuthPath_SkipsRenewal()
    {
        var (middleware, identityClient, _) = BuildSut();
        var ctx = BuildContext(path: "/bff/auth/me", expiresAt: AlreadyExpired);

        await InvokeAsync(middleware, ctx, identityClient);

        // Identity client should not have been called.
        Assert.Null(ctx.Items[HttpContextTokenExtensions.RefreshedAccessTokenKey]);
    }

    [Fact]
    public async Task InvokeAsync_WhenNotAuthenticated_SkipsRenewal()
    {
        var (middleware, identityClient, _) = BuildSut();
        var ctx = BuildContext(expiresAt: AlreadyExpired, authenticated: false);

        await InvokeAsync(middleware, ctx, identityClient);

        Assert.Null(ctx.Items[HttpContextTokenExtensions.RefreshedAccessTokenKey]);
    }

    [Fact]
    public async Task InvokeAsync_WhenTokenFarFromExpiry_SkipsRenewal()
    {
        var (middleware, identityClient, _) = BuildSut();
        var ctx = BuildContext(expiresAt: FarFuture);

        await InvokeAsync(middleware, ctx, identityClient);

        Assert.Null(ctx.Items[HttpContextTokenExtensions.RefreshedAccessTokenKey]);
    }

    // ── Renewal triggered ─────────────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_WhenTokenExpiringSoon_StoresNewTokenInItems()
    {
        const string newToken = "new_access_token";
        var tokenPair = MakeTokenPair(newToken);
        var (middleware, identityClient, _) = BuildSut(upstreamResponse: BuildJsonResponse(tokenPair));
        var ctx = BuildContext(expiresAt: SoonExpiry);

        await InvokeAsync(middleware, ctx, identityClient);

        Assert.Equal(newToken, ctx.Items[HttpContextTokenExtensions.RefreshedAccessTokenKey]);
    }

    [Fact]
    public async Task InvokeAsync_WhenTokenAlreadyExpired_StoresNewTokenInItems()
    {
        const string newToken = "new_access_token";
        var tokenPair = MakeTokenPair(newToken);
        var (middleware, identityClient, _) = BuildSut(upstreamResponse: BuildJsonResponse(tokenPair));
        var ctx = BuildContext(expiresAt: AlreadyExpired);

        await InvokeAsync(middleware, ctx, identityClient);

        Assert.Equal(newToken, ctx.Items[HttpContextTokenExtensions.RefreshedAccessTokenKey]);
    }

    [Fact]
    public async Task InvokeAsync_WhenTokenExpiringSoon_CallsNextMiddleware()
    {
        var tokenPair = MakeTokenPair("new_token");
        var nextCalled = false;

        // Build middleware with a custom _next delegate to capture the call.
        var handler = new DelegatingHandlerStub(_ => BuildJsonResponse(tokenPair));
        var identityClient = new IdentityApiClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://host/") });
        var authService = Substitute.For<IAuthenticationService>();
        authService.SignInAsync(Arg.Any<HttpContext>(), Arg.Any<string?>(),
            Arg.Any<ClaimsPrincipal>(), Arg.Any<AuthenticationProperties>())
            .Returns(Task.CompletedTask);
        authService.AuthenticateAsync(Arg.Any<HttpContext>(), Arg.Any<string?>())
            .Returns(AuthenticateResult.NoResult());

        var middleware = new SilentTokenRenewalMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            Substitute.For<ILogger<SilentTokenRenewalMiddleware>>());

        var ctx = BuildContext(expiresAt: SoonExpiry, authService: authService);
        await middleware.InvokeAsync(
            ctx, identityClient, new TokenRenewalLockProvider(),
            Options.Create(new SilentTokenRenewalOptions()));

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WhenTokenExpiringSoon_CallsSignIn()
    {
        var tokenPair = MakeTokenPair("new_token");
        var (middleware, identityClient, authService) = BuildSut(upstreamResponse: BuildJsonResponse(tokenPair));
        var ctx = BuildContext(expiresAt: SoonExpiry, authService: authService);

        await InvokeAsync(middleware, ctx, identityClient);

        await authService.Received(1).SignInAsync(
            Arg.Any<HttpContext>(),
            CookieAuthenticationDefaults.AuthenticationScheme,
            Arg.Any<ClaimsPrincipal>(),
            Arg.Any<AuthenticationProperties>());
    }

    // ── Failure paths ─────────────────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_WhenHostReturns401_ContinuesWithoutStoringToken()
    {
        var (middleware, identityClient, _) = BuildSut(
            upstreamResponse: new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var ctx = BuildContext(expiresAt: SoonExpiry);

        await InvokeAsync(middleware, ctx, identityClient);

        Assert.Null(ctx.Items[HttpContextTokenExtensions.RefreshedAccessTokenKey]);
    }

    [Fact]
    public async Task InvokeAsync_WhenNoRefreshTokenInSession_ContinuesWithoutRenewal()
    {
        var (middleware, identityClient, _) = BuildSut();
        var ctx = BuildContext(expiresAt: SoonExpiry, refreshToken: null);

        await InvokeAsync(middleware, ctx, identityClient);

        Assert.Null(ctx.Items[HttpContextTokenExtensions.RefreshedAccessTokenKey]);
    }

    // ── HttpContextTokenExtensions ────────────────────────────────────────────

    [Fact]
    public async Task GetCurrentAccessTokenAsync_WithItemOverride_ReturnsOverrideValue()
    {
        var ctx = BuildContext(expiresAt: FarFuture);
        ctx.Items[HttpContextTokenExtensions.RefreshedAccessTokenKey] = "overridden_token";

        var result = await ctx.GetCurrentAccessTokenAsync();

        Assert.Equal("overridden_token", result);
    }

    [Fact]
    public async Task GetCurrentAccessTokenAsync_WithoutOverride_ReturnsSessionToken()
    {
        var ctx = BuildContext(expiresAt: FarFuture);

        var result = await ctx.GetCurrentAccessTokenAsync();

        Assert.Equal(FakeAccessToken, result);
    }

    // ── Factories ─────────────────────────────────────────────────────────────

    private static (SilentTokenRenewalMiddleware middleware, IdentityApiClient identityClient, IAuthenticationService authService) BuildSut(
        HttpResponseMessage? upstreamResponse = null)
    {
        var handler = new DelegatingHandlerStub(_ =>
            upstreamResponse ?? new HttpResponseMessage(HttpStatusCode.OK));

        var identityClient = new IdentityApiClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://host/") });

        var authService = Substitute.For<IAuthenticationService>();
        // SignIn stub — always succeeds.
        authService.SignInAsync(
            Arg.Any<HttpContext>(), Arg.Any<string?>(),
            Arg.Any<ClaimsPrincipal>(), Arg.Any<AuthenticationProperties>())
            .Returns(Task.CompletedTask);

        var middleware = new SilentTokenRenewalMiddleware(
            _ => Task.CompletedTask,
            Substitute.For<ILogger<SilentTokenRenewalMiddleware>>());

        return (middleware, identityClient, authService);
    }

    private static DefaultHttpContext BuildContext(
        string? path = "/bff/teams",
        DateTime? expiresAt = null,
        bool authenticated = true,
        string? refreshToken = FakeRefreshToken,
        IAuthenticationService? authService = null)
    {
        var expiry = (expiresAt ?? FarFuture).ToString("O");
        var tokens = new Dictionary<string, string?>
        {
            [".Token.access_token"]            = FakeAccessToken,
            [".Token.access_token_expires_at"] = expiry,
        };
        if (refreshToken is not null)
            tokens[".Token.refresh_token"] = refreshToken;

        var properties = new AuthenticationProperties(tokens);

        authService ??= Substitute.For<IAuthenticationService>();
        authService.AuthenticateAsync(Arg.Any<HttpContext>(), Arg.Any<string?>())
            .Returns(authenticated
                ? AuthenticateResult.Success(new AuthenticationTicket(
                    new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim("sub", Guid.NewGuid().ToString("N")) },
                        CookieAuthenticationDefaults.AuthenticationScheme)),
                    properties,
                    CookieAuthenticationDefaults.AuthenticationScheme))
                : AuthenticateResult.NoResult());

        var services = new ServiceCollection();
        services.AddSingleton(authService);

        var ctx = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
        };
        ctx.Request.Path = path;

        if (authenticated)
        {
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim("sub", Guid.NewGuid().ToString("N")) },
                CookieAuthenticationDefaults.AuthenticationScheme));
        }

        return ctx;
    }

    private static Task InvokeAsync(
        SilentTokenRenewalMiddleware middleware,
        HttpContext ctx,
        IdentityApiClient client)
        => middleware.InvokeAsync(
            ctx, client, new TokenRenewalLockProvider(),
            Options.Create(new SilentTokenRenewalOptions()));

    private static HostTokenPairResponse MakeTokenPair(string accessToken) =>
        new(accessToken, DateTime.UtcNow.AddHours(8), FakeRefreshToken, DateTime.UtcNow.AddDays(7));

    private static HttpResponseMessage BuildJsonResponse<T>(T value)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(value, CamelCase);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
    }

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
