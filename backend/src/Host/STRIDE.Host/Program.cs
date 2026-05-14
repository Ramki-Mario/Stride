using Serilog;
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

// ── Auth (stubs — full implementation in Phase 2) ─────────────────────────
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

var app = builder.Build();

// ── Middleware Pipeline ────────────────────────────────────────────────────
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
