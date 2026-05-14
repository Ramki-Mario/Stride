# STRIDE Current Status

## Current Stage
Phase 1 (Monorepo & Foundation) — **Complete**.
Phase 2 (Identity & Tenant Foundation) — **In Progress**.

---

# Completed

## Phase 0 — AI Engineering Workspace
- Product naming finalized
- Architecture finalized
- SaaS strategy finalized
- Modular monolith decision finalized
- BFF architecture finalized (STRIDE.BFF dedicated project)
- Multi-tenancy strategy finalized
- Backend module hierarchy finalized (BuildingBlocks / Modules / Host / BFF / Gateway)
- Internal module layers defined (API / Application / Domain / Infrastructure)
- MediatR selected for commands, queries, domain events
- Queue abstractions decided (IDomainEvent, IIntegrationEvent, IEventBus, IBackgroundTaskQueue — Phase 1 interfaces only)
- EF Core + Dapper split strategy finalized
- Frontend structure finalized
- Observability strategy finalized
- AI governance strategy finalized
- Technology stack finalized
- All 5 architectural ambiguities resolved (2026-05-14)
- All 11 agent files populated with substantive content
- architecture.md rewritten with full module hierarchy
- discussions.md updated with ADR log (ADR-001 through ADR-015)
- ADR-011: BFF ↔ Host via typed HttpClient (YARP deferred)
- ADR-012: SQL schema separation per module (identity.*, workflows.*, etc.)
- ADR-013: Custom Result<T> in BuildingBlocks (no external NuGet)
- ADR-014: MediatR-backed in-process IEventBus for Phase 1–3
- ADR-015: Angular standalone components + Signals/RxJS hybrid

## Phase 1 — Monorepo & Foundation (Completed 2026-05-14)

### Toolchain Installed
- .NET SDK 10.0.300 (installed via winget — upgraded from 7.0 during scaffold)
- Node.js 26.1.0 (installed via winget)
- npm 11.14.1 (updated via npm install -g)
- Angular CLI 21.2.11 (installed via npm)
- NOTE: Backend initially scaffolded with net7.0, then retargeted to net10.0 after toolchain upgrade

### Infrastructure
- Monorepo folder structure: /backend, /frontend, /infrastructure, /docker, /docs, /scripts
- Docker Compose: stride-host, stride-bff, sqlserver (2022), redis (7-alpine), seq (with health checks)
- NOTE: Docker Desktop uninstalled (2026-05-15) — 8GB RAM insufficient. Local dev uses local SQL Server + Redis Cloud + console logging (no Seq)
- Dockerfiles: docker/stride-host/Dockerfile, docker/stride-bff/Dockerfile (targeting .NET 10 runtime)
- .env.example created (all required environment variables documented)
- .gitignore created (secrets, bin/obj, node_modules, dist excluded)
- CI/CD skeleton: .github/workflows/ci.yml (build + test for backend and frontend, no deploy steps)

### Backend (backend/STRIDE.sln — 50 projects, builds clean on .NET 10)
- STRIDE.sln created with all 50 projects registered
- BuildingBlocks.Domain: IDomainEvent, BaseEntity<TId>, AuditableEntity, ValueObject
- BuildingBlocks.Application: IIntegrationEvent, IEventBus, IBackgroundTaskQueue, ITenantContext, ICurrentUser, Result<T>/Result, LoggingBehaviour, ValidationBehaviour
- BuildingBlocks.Infrastructure: MediatREventBus, TenantMiddleware, TenantContextProvider, ITenantResolver, CorrelationIdMiddleware, TenantAwareRepository<T,TContext>, InfrastructureServiceExtensions
- All 7 modules scaffolded (Identity, Workflows, Scheduling, Reporting, Notifications, Invoicing, Administration)
  - Each module: 4 layers (Domain, Application, Infrastructure, API) with proper folder skeleton
  - Each module DbContext with SQL schema separation (e.g. identity.*, workflows.*)
  - Each module: ApplicationExtensions + InfrastructureExtensions + ModuleExtensions (DI registration)
  - Each module API: health controller stub (/api/{module}/health)
- STRIDE.Host: Program.cs with all modules registered, middleware pipeline (Serilog, CorrelationId, TenantMiddleware), health endpoints (/health, /health/live, /health/ready)
- STRIDE.BFF: Program.cs with typed HttpClient registration (IdentityApiClient), CORS for SPA, session config
- STRIDE.Gateway: stub only (not implemented)
- 15 test project stubs (xUnit): BuildingBlocks.Tests.Unit, per-module Domain + Application tests, Integration.Tests
- Dependency graph verified: API → Application only (controllers); API refs Infrastructure only for DI composition

### Frontend (frontend/ — Angular 21 + Tailwind 4 + PrimeNG 21)
- Angular 21.2 workspace initialized (standalone components by default)
- Tailwind CSS 4.3 installed (CSS-first, @import "tailwindcss" in styles.scss)
- PrimeNG 21.1.7 + PrimeIcons 7.0 installed
- app.config.ts: provideRouter, provideHttpClient (with interceptors), provideAnimationsAsync
- app.routes.ts: lazy routes for all 7 features, auth guard at root, shell layout wrapper
- core/auth: AuthService (Signals-based, calls BFF /auth/me, /auth/login, /auth/logout)
- core/tenant: TenantService (computed from AuthService signal)
- core/guards: authGuard (functional guard, checks session via BFF)
- core/http: correlationInterceptor, errorInterceptor (401→login, 403→forbidden)
- layout: ShellComponent, SidebarComponent, TopbarComponent (stubs — Phase 2)
- features: auth, dashboard, workflows, scheduling, reporting, notifications, administration
  - Each: components/, pages/, services/, models/, store/ subdirectories
  - Each: lazy-loaded routes file + placeholder page component
- Angular build compiles cleanly (ng build --configuration=development)

---

# Pending

## Phase 2 — Identity & Tenant Foundation — **In Progress**

### Completed
- [x] US-018: User, Role, Permission, UserRole, RolePermission entities (PR #32)
- [x] US-019: Password value object + IPasswordHasher abstraction (delivered in PR #32)
- [x] US-020: Tenant, TenantDomainMapping, UserTenantMapping entities (PR #33)
- [x] US-021: ITenantResolver + TenantResolver (Dapper-based, corporate domain + fallback) (PR #34)
- [x] US-022: EF Core configurations + InitialCreate migration + DefaultRoles seed constants (PR #35)
- [x] US-023: IUserRepository, IRoleRepository, ITenantRepository implementations (PR #38)

### Pending
- [ ] US-024: JWT strategy + BFF auth endpoints (login, logout, me) + Redis session store
- [ ] US-025: JWT configuration via appsettings
- [ ] US-026: MediatR command and query handlers (Login, Register, AssignRole, GetUser)
- [ ] US-027: FluentValidation validators for commands
- [ ] US-028: Identity API controllers (UsersController, AuthController)
- [ ] US-029: RBAC claims-based authorization policies
- [ ] **US-034: UI/UX Design ← prerequisite before any Angular UI story (GitHub #37)**
- [ ] US-030: Angular login page (PrimeNG reactive form) ← blocked on US-034
- [ ] US-031: Auth guard + APP_INITIALIZER session check
- [ ] US-032: appsettings.Development.json for local dev
- [ ] **US-033: Architecture diagrams — due end of Phase 2 (GitHub #36)**

## Phase 3 — Core Workflow Engine (Pending)
## Phase 4 — Dashboard & Reporting (Pending)
## Phase 5 — Notifications & Observability (Pending)
## Phase 6 — SaaS Readiness (Pending)
## Phase 7 — Portfolio & Deployment Polish (Pending)

---

# Architectural Decisions Finalized

| ADR | Decision |
|---|---|
| ADR-001 | Modular Monolith: BuildingBlocks / Modules / Host / BFF / Gateway |
| ADR-002 | BFF = dedicated STRIDE.BFF project |
| ADR-003 | MediatR for commands, queries, domain events |
| ADR-004 | Queue abstractions scaffolded in Phase 1 (interfaces only) |
| ADR-005 | EF Core for writes, Dapper for reporting reads |
| ADR-011 | BFF ↔ Host via typed HttpClient; YARP deferred |
| ADR-012 | SQL schema per module (identity.*, workflows.*, etc.) |
| ADR-013 | Custom Result<T> in BuildingBlocks — no external NuGet |
| ADR-014 | MediatR-backed in-process IEventBus for Phase 1–3 |
| ADR-015 | Angular standalone components; Signals + RxJS hybrid |
