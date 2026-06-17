using Microsoft.AspNetCore.Authentication.Cookies;
using Serilog;
using StackExchange.Redis;
using STRIDE.BFF.Auth;
using STRIDE.BFF.Hubs;
using STRIDE.BFF.HttpClients;
using STRIDE.BFF.Middleware;
using STRIDE.BFF.Realtime;
using STRIDE.BuildingBlocks.Infrastructure.Correlation;
using STRIDE.BuildingBlocks.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ── Logging ────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration));

// ── Building Blocks ────────────────────────────────────────────────────────
builder.Services.AddBuildingBlocksInfrastructure(builder.Configuration);

// ── Typed HTTP Clients → STRIDE.Host (ADR-011) ────────────────────────────
var hostBaseUrl = builder.Configuration["Services:StrideHost"]
    ?? throw new InvalidOperationException("Services:StrideHost is not configured.");

builder.Services.AddHttpClient<IdentityApiClient>(client =>
    client.BaseAddress = new Uri(hostBaseUrl));

builder.Services.AddHttpClient<ReportingApiClient>(client =>
    client.BaseAddress = new Uri(hostBaseUrl));

builder.Services.AddHttpClient<WorkflowApiClient>(client =>
    client.BaseAddress = new Uri(hostBaseUrl));

builder.Services.AddHttpClient<NotificationsApiClient>(client =>
    client.BaseAddress = new Uri(hostBaseUrl));

builder.Services.AddHttpClient<HealthApiClient>(client =>
    client.BaseAddress = new Uri(hostBaseUrl));

builder.Services.AddHttpClient<AdminApiClient>(client =>
    client.BaseAddress = new Uri(hostBaseUrl));

builder.Services.AddHttpClient<InvoicingApiClient>(client =>
    client.BaseAddress = new Uri(hostBaseUrl));

builder.Services.AddHttpClient<ClientsApiClient>(client =>
    client.BaseAddress = new Uri(hostBaseUrl));

builder.Services.AddHttpClient<TeamsApiClient>(client =>
    client.BaseAddress = new Uri(hostBaseUrl));

builder.Services.AddHttpClient<TenantSettingsApiClient>(client =>
    client.BaseAddress = new Uri(hostBaseUrl));

builder.Services.AddHttpClient<TenantRegistrationApiClient>(client =>
    client.BaseAddress = new Uri(hostBaseUrl));

builder.Services.AddHttpClient<WebhooksApiClient>(client =>
    client.BaseAddress = new Uri(hostBaseUrl));

builder.Services.AddHttpClient<KitOpsApiClient>(client =>
    client.BaseAddress = new Uri(hostBaseUrl));

builder.Services.AddHttpClient<SchedulingApiClient>(client =>
    client.BaseAddress = new Uri(hostBaseUrl));

// ── Redis (session store) ──────────────────────────────────────────────────
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection(RedisOptions.SectionName));

var redisConnection = builder.Configuration[$"{RedisOptions.SectionName}:ConnectionString"]
    ?? throw new InvalidOperationException("Redis:ConnectionString is not configured.");

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var opts = ConfigurationOptions.Parse(redisConnection);
    // sslProtocols is not a valid connection string token in StackExchange.Redis —
    // must be set here via ConfigurationOptions.
    opts.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
    // Redis Cloud free-tier cert CN does not match the public hostname —
    // bypass validation so the TLS handshake can complete.
    opts.CertificateValidation += (_, _, _, _) => true;
    return ConnectionMultiplexer.Connect(opts);
});

builder.Services.AddSingleton<ITicketStore, RedisTicketStore>();
builder.Services.AddSingleton<TokenRenewalLockProvider>();
builder.Services.Configure<SilentTokenRenewalOptions>(
    builder.Configuration.GetSection(SilentTokenRenewalOptions.SectionName));

// ── Cookie Auth backed by Redis ticket store ──────────────────────────────
// Browser only ever sees an opaque cookie; the JWT lives in Redis.
builder.Services
    .AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
    .Configure<IServiceProvider>((options, sp) =>
    {
        options.Cookie.Name = "__strydesuite_session";
        options.Cookie.HttpOnly = true;
        // Lax (not Strict) so the cookie is sent when the user navigates here
        // from an external link (email, bookmark, SSO redirect). Strict would
        // silently drop the cookie on first-party top-level navigation.
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Cookie.IsEssential = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = false;
        options.SessionStore = sp.GetRequiredService<ITicketStore>();

        // SPA-friendly: never 302-redirect on auth failure — return status codes.
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie();

builder.Services.AddAuthorization();

// ── CORS: allow Angular SPA origin only ───────────────────────────────────
builder.Services.AddCors(opts => opts.AddPolicy("SPA", policy =>
    policy.WithOrigins(
        builder.Configuration["Cors:SpaOrigin"]
            ?? "http://localhost:4200")
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials()));

builder.Services.AddControllers();
builder.Services.AddHealthChecks();

// ── Real-time dashboard (US-176) ──────────────────────────────────────────
// SignalR hub authenticated by the session cookie; a hosted service bridges
// Redis pub/sub messages from the Host into tenant-scoped hub groups.
builder.Services.AddSignalR();
builder.Services.AddHostedService<DashboardRedisSubscriber>();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseCors("SPA");
app.UseAuthentication();
app.UseMiddleware<SilentTokenRenewalMiddleware>(); // runs after auth, before controllers
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHub<DashboardHub>("/bff/hubs/dashboard");

await app.RunAsync();
