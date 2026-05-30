using System.Threading.RateLimiting;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StackExchange.Redis;
using System.Security.Authentication;
using STRIDE.Host.ErrorHandling;
using STRIDE.BuildingBlocks.Infrastructure.Correlation;
using STRIDE.BuildingBlocks.Infrastructure.Extensions;
using STRIDE.BuildingBlocks.Infrastructure.Logging;
using STRIDE.BuildingBlocks.Infrastructure.Tenant;
using STRIDE.Modules.Identity.API.Extensions;
using STRIDE.Modules.Identity.Infrastructure.Persistence.SeedData;
using STRIDE.Modules.Workflows.Infrastructure.Persistence.SeedData;
using STRIDE.Modules.Workflows.API.Extensions;
using STRIDE.Modules.Scheduling.API.Extensions;
using STRIDE.Modules.Reporting.API.Extensions;
using STRIDE.Modules.Notifications.API.Extensions;
using STRIDE.Modules.Invoicing.API.Extensions;
using STRIDE.Modules.Administration.API.Extensions;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ── Logging ────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration));

// ── Building Blocks + Modules ──────────────────────────────────────────────
builder.Services
    .AddBuildingBlocksInfrastructure(builder.Configuration)
    .AddIdentityModule(builder.Configuration)
    .AddWorkflowsModule(builder.Configuration)
    .AddSchedulingModule(builder.Configuration)
    .AddReportingModule(builder.Configuration)
    .AddNotificationsModule(builder.Configuration)
    .AddInvoicingModule(builder.Configuration)
    .AddAdministrationModule(builder.Configuration)
    // IEventBus (MediatREventBus) depends on IPublisher — registered here AFTER
    // all modules have called AddMediatR() so IPublisher is already in the container.
    // This is intentionally NOT called in the BFF (BFF has no MediatR handlers).
    .AddBuildingBlocksEventBus();

// ── Controllers (all module API assemblies registered as application parts) ─
builder.Services
    .AddControllers()
    // Serialize C# enums as their string names (e.g. "DashboardKpi", "Draft")
    // rather than integer values. Ensures the Angular client can send/receive
    // enum fields by name without needing a custom mapping layer.
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .AddApplicationPart(typeof(IdentityModuleExtensions).Assembly)
    .AddApplicationPart(typeof(WorkflowsModuleExtensions).Assembly)
    .AddApplicationPart(typeof(SchedulingModuleExtensions).Assembly)
    .AddApplicationPart(typeof(ReportingModuleExtensions).Assembly)
    .AddApplicationPart(typeof(NotificationsModuleExtensions).Assembly)
    .AddApplicationPart(typeof(InvoicingModuleExtensions).Assembly)
    .AddApplicationPart(typeof(AdministrationModuleExtensions).Assembly);

// ── Health Checks ──────────────────────────────────────────────────────────
// SQL Server + Redis checks tagged "ready" so /health/ready includes them.
// /health/live is a liveness probe (no dependency checks — always 200 if the
// process is running). /health reports everything with verbose JSON output.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
var redisConnStr     = builder.Configuration["Redis:ConnectionString"]!;

// Register IConnectionMultiplexer with the same TLS cert bypass used by the BFF.
//
// Redis Cloud's cert CN does not match its public hostname.  The bypass only
// works when SSL is enabled *exclusively* via SslProtocols — NOT via ssl=True
// in the connection string.  If the connection string contains ssl=True,
// ConfigurationOptions.Parse sets opts.Ssl = true, which activates a separate
// TLS negotiate path that runs before our CertificateValidation event fires.
//
// Fix: parse the string first, then force opts.Ssl = false so SSL is owned
// entirely by SslProtocols = Tls12 — identical to the BFF which works.
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var opts = ConfigurationOptions.Parse(redisConnStr);
    opts.Ssl               = false;            // clear ssl=True if present in conn string
    opts.SslProtocols      = SslProtocols.Tls12; // re-enable SSL via protocol (BFF code path)
    opts.CertificateValidation += (_, _, _, _) => true; // bypass CN mismatch
    return ConnectionMultiplexer.Connect(opts);
});

builder.Services
    .AddHealthChecks()
    .AddSqlServer(
        connectionString: connectionString,
        name:             "sql-server",
        tags:             ["ready", "db"])
    .AddRedis(
        // Use the pre-configured singleton — NOT a raw connection string — so the
        // TLS cert bypass above applies. Passing a raw string here bypasses the fix.
        connectionMultiplexerFactory: sp => sp.GetRequiredService<IConnectionMultiplexer>(),
        name:                         "redis",
        tags:                         ["ready", "cache"]);

// ── Rate Limiting ──────────────────────────────────────────────────────────
// Per-tenant sliding window: 100 req / 60 s, keyed on the JWT "tid" claim.
// Anonymous requests (no "tid") share a single global bucket.
// Rejected requests receive 429 Too Many Requests + Retry-After: 60 header.
builder.Services.AddRateLimiter(options =>
{
    options.OnRejected = async (ctx, ct) =>
    {
        ctx.HttpContext.Response.StatusCode  = StatusCodes.Status429TooManyRequests;
        ctx.HttpContext.Response.Headers["Retry-After"] = "60";
        ctx.HttpContext.Response.ContentType = "application/json";
        await ctx.HttpContext.Response.WriteAsync(
            "{\"error\":\"Too many requests. Please retry after 60 seconds.\"}", ct);
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
    {
        // Authenticated requests are partitioned per tenant; anonymous by IP.
        var partitionKey = ctx.User.FindFirst("tid")?.Value
            ?? ctx.Connection.RemoteIpAddress?.ToString()
            ?? "anonymous";

        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey,
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit          = 100,
                Window               = TimeSpan.FromSeconds(60),
                SegmentsPerWindow    = 6,   // 10-second resolution
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = 0,
            });
    });
});

// ── Global Exception Handler ───────────────────────────────────────────────
// Converts ValidationException → 400 (RFC 7807) and unhandled → 500.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ── Auth — JWT Bearer ─────────────────────────────────────────────────────
// Host validates Bearer tokens issued by JwtTokenService (HS256).
// Secret is supplied via environment variable Jwt__Secret (see .env.example).
// Never embed the production secret in appsettings files committed to source.
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException(
        "Jwt:Secret is not configured. " +
        "Set the Jwt__Secret environment variable or add it to appsettings.Development.json. " +
        "See .env.example for generation instructions.");

var jwtIssuer   = builder.Configuration["Jwt:Issuer"]   ?? "STRIDE";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "STRIDE.Clients";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Disable the default InboundClaimTypeMap which silently renames JWT claim
        // names to long-form Microsoft URIs (e.g. "tid" → ms/identity/claims/tenantid).
        // With this off, every claim name in context.User matches what JwtTokenService wrote.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtIssuer,
            ValidAudience            = jwtAudience,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            // With MapInboundClaims = false, claim types stay as issued by JwtTokenService:
            //   "sub"            → UserId
            //   "email"          → Email (short JWT name, not ClaimTypes.Email URI)
            //   "tid"            → TenantId (our custom claim, no longer remapped)
            //   ClaimTypes.Role  → Roles (stored with full URI by JwtTokenService)
            NameClaimType            = "sub",
            RoleClaimType            = ClaimTypes.Role,
            // 30-second clock skew tolerates minor time drift between services.
            ClockSkew                = TimeSpan.FromSeconds(30),
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// ── Development seed data ──────────────────────────────────────────────────
// Identity seed: test@gmail.com / 1234abcd + Admin role + Tenant (idempotent).
// Workflow seed: 5 definitions + ~20 instances spread over 30 days (idempotent).
// Both are no-ops outside the Development environment.
var identitySeed = await DevDataSeeder.SeedAsync(app);
if (identitySeed is var (seedTenantId, seedUserId))
    await WorkflowDevDataSeeder.SeedAsync(app, seedTenantId, seedUserId);

// ── Middleware Pipeline ────────────────────────────────────────────────────
app.UseExceptionHandler();   // Must be first so it wraps all downstream middleware.
app.UseRateLimiter();        // Apply global tenant rate limit before auth.
app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseAuthentication();                     // Must run before TenantMiddleware — populates context.User from JWT.
app.UseMiddleware<TenantMiddleware>();        // Reads "tid" claim from the now-populated context.User.
app.UseMiddleware<SerilogEnrichmentMiddleware>(); // Enriches every log line with UserId + TenantId from JWT.
app.UseAuthorization();

app.MapControllers();

// /health      — all checks, verbose JSON (for dev / monitoring dashboards)
// /health/live — liveness probe: always 200 if the process responds (no dependency checks)
// /health/ready — readiness probe: SQL Server + Redis must be reachable
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate      = _ => false,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate      = r => r.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
});

app.Run();
