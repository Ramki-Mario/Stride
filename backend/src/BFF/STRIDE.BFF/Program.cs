using Serilog;
using STRIDE.BuildingBlocks.Infrastructure.Correlation;
using STRIDE.BuildingBlocks.Infrastructure.Extensions;
using STRIDE.BFF.HttpClients;

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

// ── Session / Cookie Auth (full implementation in Phase 2) ────────────────
builder.Services.AddAuthentication();
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

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseCors("SPA");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
