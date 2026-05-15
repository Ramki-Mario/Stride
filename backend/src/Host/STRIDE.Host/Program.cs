using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using STRIDE.Host.ErrorHandling;
using STRIDE.BuildingBlocks.Infrastructure.Correlation;
using STRIDE.BuildingBlocks.Infrastructure.Extensions;
using STRIDE.BuildingBlocks.Infrastructure.Tenant;
using STRIDE.Modules.Identity.API.Extensions;
using STRIDE.Modules.Workflows.API.Extensions;
using STRIDE.Modules.Scheduling.API.Extensions;
using STRIDE.Modules.Reporting.API.Extensions;
using STRIDE.Modules.Notifications.API.Extensions;
using STRIDE.Modules.Invoicing.API.Extensions;
using STRIDE.Modules.Administration.API.Extensions;
using System.Text;

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
    .AddAdministrationModule(builder.Configuration);

// ── Controllers (all module API assemblies registered as application parts) ─
builder.Services
    .AddControllers()
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
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtIssuer,
            ValidAudience            = jwtAudience,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            // "sub" is mapped to ClaimTypes.NameIdentifier by the JWT middleware by default.
            // NameClaimType / RoleClaimType align handler with JwtTokenService claim shapes.
            NameClaimType            = "sub",
            RoleClaimType            = "role",
            // 30-second clock skew tolerates minor time drift between services.
            ClockSkew                = TimeSpan.FromSeconds(30),
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

// ── Middleware Pipeline ────────────────────────────────────────────────────
app.UseExceptionHandler();   // Must be first so it wraps all downstream middleware.
app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live",  new() { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new() { Predicate = r => r.Tags.Contains("ready") });

app.Run();
