using Microsoft.AspNetCore.Authentication.Cookies;
using Serilog;
using StackExchange.Redis;
using STRIDE.BFF.Auth;
using STRIDE.BFF.HttpClients;
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

// Additional typed clients added here as modules are implemented:
// builder.Services.AddHttpClient<WorkflowApiClient>(...);
// builder.Services.AddHttpClient<SchedulingApiClient>(...);

// ── Session Store ──────────────────────────────────────────────────────────
// Development: in-memory store — no Redis required, sessions lost on restart.
// Production:  Redis-backed store — shared across instances, server-side revocable.
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection(RedisOptions.SectionName));

if (builder.Environment.IsDevelopment())
{
    // IMemoryCache is needed by InMemoryTicketStore.
    builder.Services.AddMemoryCache();
    builder.Services.AddSingleton<ITicketStore, InMemoryTicketStore>();
}
else
{
    var redisConnection = builder.Configuration[$"{RedisOptions.SectionName}:ConnectionString"]
        ?? throw new InvalidOperationException("Redis:ConnectionString is not configured.");

    builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    {
        var opts = ConfigurationOptions.Parse(redisConnection);
        opts.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
        opts.CertificateValidation += (_, _, _, _) => true;
        return ConnectionMultiplexer.Connect(opts);
    });

    builder.Services.AddSingleton<ITicketStore, RedisTicketStore>();
}

// ── Cookie Auth backed by the environment-appropriate ticket store ─────────
// Browser only ever sees an opaque key cookie; the ticket lives server-side.
builder.Services
    .AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
    .Configure<IServiceProvider>((options, sp) =>
    {
        options.Cookie.Name = "stride.session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
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

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseCors("SPA");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
