<div align="center">

# STRIDE

**Turn your standard operating procedures into trackable, billable workflows.**

[![CI](https://github.com/Ramki-Mario/Stride/actions/workflows/ci.yml/badge.svg?branch=develop)](https://github.com/Ramki-Mario/Stride/actions/workflows/ci.yml)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=Ramki-Mario_Stride&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=Ramki-Mario_Stride)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=Ramki-Mario_Stride&metric=coverage)](https://sonarcloud.io/summary/new_code?id=Ramki-Mario_Stride)
[![Maintainability](https://sonarcloud.io/api/project_badges/measure?project=Ramki-Mario_Stride&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=Ramki-Mario_Stride)

</div>

---

## The Problem

Professional service businesses — legal firms, accountants, consulting practices, managed service providers — run on repeatable processes. Onboarding a client. Preparing a tax return. Completing a project milestone. Issuing an invoice.

Without purpose-built tooling, these processes live in email threads, shared spreadsheets, and the heads of whoever's been around longest. The result:

- **Steps are missed** because no one knows who owns what.
- **Deadlines are invisible** until someone complains.
- **Invoicing lags** because there is no automatic signal that a job is done.
- **Managers spend more time chasing status updates than doing work.**

Generic project tools like Jira are built for software teams; Asana and Monday are built for marketing campaigns. Neither understands SLAs, per-step assignments, billable milestones, or multi-tenant client isolation out of the box.

---

## The Solution

STRIDE is a multi-tenant SaaS platform that converts your firm's standard operating procedures into structured, assignable, trackable workflows — and automatically turns completed work into invoices.

A manager defines a workflow template (e.g. "Annual Account Preparation"), with steps, owners, deadlines, and billable line items. Each time a client engagement starts, STRIDE spins up a live instance. Staff work through their assigned steps; managers see real-time progress on a dashboard; when the final step is approved, an invoice draft is generated automatically and sent to the client for sign-off.

**Core capabilities:**

| Capability | What it does |
|---|---|
| Workflow Engine | Define reusable templates; run concurrent instances per client |
| Step Assignments | Assign steps by user or role with optional SLA deadlines |
| RBAC | Multi-tenant role hierarchy with fine-grained permissions |
| Real-time Dashboard | KPI strip, overdue alerts, team workload, unassigned steps |
| Invoice Automation | Auto-draft invoices from completed workflows; client sign-off portal |
| Operational Analytics | Completion-time trends, team performance leaderboard, revenue analytics |
| Webhooks | Outbound events with HMAC-SHA256 signing and exponential-backoff retry |
| Notifications | Real-time in-app push via SignalR |

---

## Key Features

- **Multi-tenant isolation** — Every tenant's data is partitioned at the database schema level; no cross-tenant data leakage.
- **Workflow templates and instances** — Design once, run many times. Templates carry step order, role requirements, and SLA rules.
- **Per-step deadlines and overdue detection** — A background job flags overdue steps and surfaces them in the dashboard alert panel.
- **Role-based step routing** — Steps can be assigned to a specific user or require any user with a given role.
- **Invoice lifecycle** — Draft → Sent → Paid → Void; line items calculated from billable workflow steps.
- **Client sign-off portal** — Shareable time-limited link for client review and sign-off before an invoice is sent.
- **BFF security pattern** — JWTs never touch the browser; session cookies are HTTP-only; the BFF holds and forwards tokens server-side.
- **Operational analytics** — Three independent analytics views: workflow completion time, team performance, and revenue from closed workflows.
- **Webhook subscriptions** — Tenants register endpoints to receive signed JSON payloads for key domain events. Failed deliveries retry with jitter up to 5 attempts.
- **Structured logging** — All services emit structured logs to Seq; correlation IDs trace requests across BFF → Host boundary.

---

## Architecture

STRIDE is a **modular monolith** using **Clean Architecture** within each module, fronted by a **BFF (Backend for Frontend)** that handles session management and token forwarding.

```
┌─────────────────────────────────────────────────────────┐
│  Angular 21 SPA  (PrimeNG · signals · OnPush)           │
└────────────────────┬────────────────────────────────────┘
                     │  HTTP (cookies, no tokens in browser)
┌────────────────────▼────────────────────────────────────┐
│  STRIDE.BFF  (ASP.NET Core — session + token proxy)     │
│  Redis session store · HTTP-only cookie auth            │
└────────────────────┬────────────────────────────────────┘
                     │  Bearer JWT (server-to-server)
┌────────────────────▼────────────────────────────────────┐
│  STRIDE.Host  (modular monolith)                        │
│  ┌──────────┐ ┌──────────┐ ┌───────────┐ ┌──────────┐  │
│  │ Identity │ │Workflows │ │ Invoicing │ │Reporting │  │
│  └──────────┘ └──────────┘ └───────────┘ └──────────┘  │
│  ┌──────────┐ ┌──────────┐ ┌───────────┐ ┌──────────┐  │
│  │ Clients  │ │  Teams   │ │ Webhooks  │ │  Notifs  │  │
│  └──────────┘ └──────────┘ └───────────┘ └──────────┘  │
└────────────────────┬────────────────────────────────────┘
                     │
        ┌────────────┴────────────┐
        │  SQL Server 2022        │  Redis 7
        │  (per-module schemas)   │  (sessions · cache)
        └─────────────────────────┘
```

Each module follows the same four-layer pattern:

| Layer | Project suffix | Responsibility |
|---|---|---|
| Domain | `.Domain` | Entities, value objects, domain events, repository interfaces |
| Application | `.Application` | Commands, queries (MediatR), validators (FluentValidation), DTOs |
| Infrastructure | `.Infrastructure` | EF Core repositories, Dapper read models, SQL scripts |
| API | `.API` | ASP.NET Core controllers, DI registration |

**Architecture diagrams** (generated with Mermaid):

| Diagram | Description |
|---|---|
| [System Context](docs/diagrams/system-context.png) | STRIDE in relation to external actors |
| [Container View](docs/diagrams/container.png) | BFF, Host, DB, Redis, Seq |
| [Module Interaction](docs/diagrams/module-interaction.png) | Cross-module event flow |
| [Auth Flow](docs/diagrams/auth-flow.png) | Login → BFF session → JWT forwarding |

---

## Getting Started

### Prerequisites

| Tool | Minimum version |
|---|---|
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | 4.x |
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | 10.0.300 |
| [Node.js](https://nodejs.org/) | 22 LTS |

> **Quickest path:** Docker Compose spins up the full stack (SQL Server, Redis, Seq, BFF, Host, Angular SPA) in one command.

---

### Option A — Docker Compose (recommended)

```bash
# 1. Clone the repository
git clone https://github.com/Ramki-Mario/Stride.git
cd Stride

# 2. Set the SQL Server SA password (must meet complexity requirements)
#    On Linux/macOS:
export SQL_SA_PASSWORD="YourStr0ngP@ssword!"
#    On Windows PowerShell:
$env:SQL_SA_PASSWORD = "YourStr0ngP@ssword!"

# 3. Build and start all services
docker compose -f docker/docker-compose.yml up --build

# 4. Open the app
#    Angular SPA   → http://localhost:4200
#    BFF API       → http://localhost:5000
#    Host API      → http://localhost:5001
#    Seq logs      → http://localhost:5341
```

> The Host runs EF Core migrations automatically on startup; no manual migration step is needed.

---

### Option B — Local development

**Backend (STRIDE.Host + STRIDE.BFF):**

```bash
# Requires a running SQL Server and Redis instance.
# Update connection strings in:
#   backend/src/Host/STRIDE.Host/appsettings.Development.json
#   backend/src/BFF/STRIDE.BFF/appsettings.Development.json

cd backend
dotnet restore STRIDE.sln
dotnet run --project src/Host/STRIDE.Host
# In a second terminal:
dotnet run --project src/BFF/STRIDE.BFF
```

**Frontend:**

```bash
cd frontend
npm ci
npx ng serve        # proxies /bff/* to http://localhost:5000
```

Open `http://localhost:4200`.

---

### First login

A dev seed creates a default tenant and admin user on first startup:

| Field | Value |
|---|---|
| Email | `admin@stride.dev` |
| Password | `Admin@123` |

> Change these credentials immediately in any non-development environment.

---

## Roadmap

The current milestone (**Product Layer Gaps**) adds depth to the existing workflow engine:

- [x] Client entity and history view
- [x] Invoice automation from completed workflows
- [x] Client sign-off portal (shareable link)
- [x] Operational analytics (completion time, team performance, revenue)
- [x] Webhook subscriptions with signed delivery and retry
- [ ] Refresh token rotation
- [ ] Invitation email flow
- [ ] Mobile-responsive UI pass
- [ ] Pre-production security review

---

## Tech Stack

<div align="center">

![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![Angular](https://img.shields.io/badge/Angular-21-DD0031?style=flat-square&logo=angular&logoColor=white)
![PrimeNG](https://img.shields.io/badge/PrimeNG-21-0EA5E9?style=flat-square&logoColor=white)
![TypeScript](https://img.shields.io/badge/TypeScript-5-3178C6?style=flat-square&logo=typescript&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-2022-CC2927?style=flat-square&logo=microsoftsqlserver&logoColor=white)
![Redis](https://img.shields.io/badge/Redis-7-DC382D?style=flat-square&logo=redis&logoColor=white)
![Docker](https://img.shields.io/badge/Docker_Compose-v2-2496ED?style=flat-square&logo=docker&logoColor=white)
![SignalR](https://img.shields.io/badge/SignalR-real--time-512BD4?style=flat-square&logo=dotnet&logoColor=white)
![Seq](https://img.shields.io/badge/Seq-structured_logs-7B42BC?style=flat-square&logoColor=white)
![GitHub Actions](https://img.shields.io/badge/GitHub_Actions-CI-2088FF?style=flat-square&logo=githubactions&logoColor=white)
![SonarCloud](https://img.shields.io/badge/SonarCloud-quality-F3702A?style=flat-square&logo=sonarcloud&logoColor=white)

</div>

---

## Contributing

This is a portfolio / product project. Issues and discussions are open; pull requests are welcome for bug fixes and documentation improvements.

---

<div align="center">
<sub>Built with .NET 10 · Angular 21 · Clean Architecture · BFF pattern</sub>
</div>
