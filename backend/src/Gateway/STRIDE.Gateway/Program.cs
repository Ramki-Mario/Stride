// STRIDE.Gateway — stub only (Phase 1).
// Full API gateway / routing implementation is deferred (see architecture.md — STRIDE.Gateway).
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.MapGet("/", () => "STRIDE Gateway — stub");
await app.RunAsync();
