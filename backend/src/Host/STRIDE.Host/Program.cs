using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using STRIDE.Host.ErrorHandling;
using STRIDE.BuildingBlocks.Infrastructure.Correlation;
using STRIDE.BuildingBlocks.Infrastructure.Extensions;
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
// SQL Server health check added in Phase 2 when Identity DB is initialized.
builder.Services.AddHealthChecks();

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
app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseAuthentication();                     // Must run before TenantMiddleware — populates context.User from JWT.
app.UseMiddleware<TenantMiddleware>();        // Reads "tid" claim from the now-populated context.User.
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live",  new() { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new() { Predicate = r => r.Tags.Contains("ready") });

app.Run();
