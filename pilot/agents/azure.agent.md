# azure.agent.md
# STRIDE — Azure Infrastructure Agent

## Purpose
Persistent AI memory for Azure infrastructure decisions.
Read before implementing any cloud resource configuration, connection string, or deployment config.

---

## Azure Services Used

| Service | Purpose |
|---|---|
| Azure SQL | Primary relational database |
| Azure Cache for Redis | Session storage, auth cache, workflow cache |
| Azure Application Insights | APM, telemetry, distributed tracing |
| Azure Container Registry | Docker image storage |
| Azure App Service / ACI | Initial hosting (pre-Kubernetes) |
| Azure Key Vault | Secrets management (production) |
| Azure Service Bus | Future: IEventBus implementation |

---

## Connection String Strategy

Never hardcode connection strings.
Development: `appsettings.Development.json` or environment variables.
Production: Azure Key Vault + environment variables injected at deployment.

```csharp
// Strongly-typed options per resource
public class DatabaseOptions
{
    public string ConnectionString { get; set; } = string.Empty;
}

public class RedisOptions
{
    public string ConnectionString { get; set; } = string.Empty;
}
```

---

## Application Insights Integration

- Use OpenTelemetry SDK with Application Insights exporter
- Configure correlation IDs to flow through distributed traces
- Instrument: HTTP requests, SQL queries, Redis operations, background jobs
- TenantId as a custom dimension on all telemetry

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddSqlClientInstrumentation()
        .AddRedisInstrumentation()
        .AddAzureMonitorTraceExporter());
```

---

## Environment Strategy

```
Development  → local Docker + local SQL/Redis
Staging      → Azure (mirrors production config)
Production   → Azure (Key Vault, managed identities)
```

Environment-specific config via `ASPNETCORE_ENVIRONMENT`.
Never commit production secrets.

---

## Docker Strategy

Phase 1: Docker Compose for local development.
Phase 2+: Dockerfile per service, Azure Container Registry.
Future: Kubernetes (AKS) when scaling demands it.

```yaml
# docker-compose.yml services (Phase 1)
services:
  stride-host:
  stride-bff:
  sqlserver:
  redis:
  seq:          # Local structured log viewer
```

---

## Future Azure Service Bus (IEventBus Implementation)

When `IEventBus` gets a real implementation:
- Use Azure Service Bus topics/subscriptions
- One topic per integration event type
- One subscription per consuming module
- Dead-letter queue monitoring required
- Message envelope includes: TenantId, CorrelationId, EventId, PublishedAt

This is OUT OF SCOPE for Phase 1-3.
The `IEventBus` interface is scaffolded now — implementation deferred.

---

## Kubernetes Readiness (Future)

Architecture decisions made now to support future AKS:
- Stateless application design (sessions in Redis, not in-memory)
- Health check endpoints (`/health/live`, `/health/ready`)
- Environment variable-based configuration (no file-system secrets)
- Docker image per service
- Graceful shutdown handling (`IHostApplicationLifetime`)

Do NOT implement AKS deployment yet.

---

## AI Constraints

MUST:
- Use strongly-typed options for all Azure config
- Reference Key Vault for secrets in production config
- Include TenantId in all telemetry custom dimensions
- Design for stateless horizontal scaling

MUST NOT:
- Hardcode connection strings anywhere
- Commit secrets or keys to source control
- Implement Service Bus or AKS infrastructure in Phase 1
- Use in-process session storage (always use Redis)
