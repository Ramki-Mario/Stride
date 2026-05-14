# deployment.agent.md
# STRIDE — Deployment Agent

## Purpose
Persistent AI memory for Docker, CI/CD, and environment configuration.
Read before implementing any Dockerfile, compose file, pipeline, or environment config.

---

## Current Deployment Target

Phase 1: Local Docker Compose (development environment only).
Phase 2+: Azure App Service or ACI.
Future: Azure Kubernetes Service (AKS).

---

## Docker Compose (Phase 1)

Services required for local development:

```yaml
services:
  stride-bff:
    build: ./backend/src/BFF/STRIDE.BFF
    ports: ["5000:8080"]
    depends_on: [stride-host, redis]

  stride-host:
    build: ./backend/src/Host/STRIDE.Host
    ports: ["5001:8080"]
    depends_on: [sqlserver, redis]

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: "${SQL_SA_PASSWORD}"
      ACCEPT_EULA: "Y"
    ports: ["1433:1433"]
    volumes: ["sqldata:/var/opt/mssql"]

  redis:
    image: redis:7-alpine
    ports: ["6379:6379"]

  seq:
    image: datalust/seq:latest
    ports: ["5341:80"]
    environment:
      ACCEPT_EULA: "Y"
```

---

## Dockerfile Pattern (per service)

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["backend/src/Host/STRIDE.Host/STRIDE.Host.csproj", "Host/STRIDE.Host/"]
# ... copy all referenced projects
RUN dotnet restore "Host/STRIDE.Host/STRIDE.Host.csproj"
COPY . .
RUN dotnet build "Host/STRIDE.Host/STRIDE.Host.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Host/STRIDE.Host/STRIDE.Host.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "STRIDE.Host.dll"]
```

---

## Environment Variables Strategy

Never store secrets in source control.

Development: `.env` file at monorepo root (gitignored).
Staging/Production: injected via CI/CD pipeline variables or Azure Key Vault.

Required environment variables:
```
ASPNETCORE_ENVIRONMENT=Development|Staging|Production
ConnectionStrings__DefaultConnection=...
ConnectionStrings__Redis=...
Jwt__Secret=...
Seq__ServerUrl=http://seq:5341
ApplicationInsights__ConnectionString=...
```

---

## CI/CD Skeleton (Phase 1)

Minimal pipeline (GitHub Actions or Azure DevOps):

```yaml
# .github/workflows/ci.yml
on: [push, pull_request]
jobs:
  build:
    steps:
      - checkout
      - dotnet restore
      - dotnet build
      - dotnet test
      - docker build (STRIDE.Host)
      - docker build (STRIDE.BFF)
```

No deployment steps in Phase 1 — build and test only.

---

## Health Check Integration

Docker Compose healthcheck per service:
```yaml
stride-host:
  healthcheck:
    test: ["CMD", "curl", "-f", "http://localhost:8080/health/live"]
    interval: 30s
    timeout: 10s
    retries: 3
```

---

## Git Strategy

Branch naming:
```
main          → production-ready
develop       → integration branch
feat/*        → feature branches
fix/*         → bugfix branches
chore/*       → tooling/config
```

Never commit directly to `main`.
PRs require passing CI before merge.

---

## AI Constraints

MUST:
- Keep Docker Compose working as the primary local dev environment
- Gitignore all `.env` files and secrets
- Include health checks in all service definitions
- Keep CI pipeline minimal in Phase 1

MUST NOT:
- Hardcode credentials in any compose or Dockerfile
- Skip health check endpoints
- Implement AKS or Service Bus deployment in Phase 1
- Create separate Dockerfiles per module (one per deployable service: Host and BFF)
