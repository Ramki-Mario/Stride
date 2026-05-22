# STRIDE Current Status

## Current Stage
Phase 1 (Monorepo & Foundation) — **Complete**.
Phase 2 (Identity & Tenant Foundation) — **Complete**.
Phase 3 (Core Workflow Engine) — **Complete**.
Phase 4 (Dashboard & Reporting) — **Complete** ✅ (all stories done, all epics closed).

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
- [x] US-024: IJwtTokenService + BFF auth endpoints (login/logout/me) + Redis ITicketStore (PR #39)
- [x] US-025: JWT bearer validation on Host + appsettings hygiene (PR #41)
- [x] US-026: MediatR handlers (Login, Register, AssignRole, GetUser) + PBKDF2 PasswordHasher + ITenantContextSetter (PR #42)
- [x] US-027: FluentValidation validators + LoggingBehaviour/ValidationBehaviour wired into MediatR pipeline (PR #43)
- [x] US-028: Identity API controllers (AuthController, UsersController) + CurrentUser + GlobalExceptionHandler (PR #44)
- [x] US-029: RBAC claims-based authorization policies (PR #45)
- [x] US-032: appsettings.Development.json for local dev (PR #54)

### Pending
- [x] **US-034: UI/UX Design — ALL 7 sub-issues Done ✅ (GitHub #37 closed, EP-015)**
  - [x] US-034.1 Shell & Layout — `01-shell-layout.html` PR #62 ✅
  - [x] US-034.2 Login Page — `02-login.html` PR #63 ✅
  - [x] US-034.3 Dashboard — `03-dashboard.html` PR #65 ✅
  - [x] US-034.4 Workflow List — `04-workflow-list.html` PR #66 ✅
  - [x] US-034.5 Workflow Detail — `05-workflow-detail.html` PR #67 ✅
  - [x] US-034.6 User Management — `06-user-management.html` PR #68 ✅
  - [x] US-034.7 Reporting — `07-reporting.html` PR #69 ✅
- [x] **US-030: Angular login page (reactive form, CSS-only spinner, login.readme.md KT) — PR #70 ✅ (2026-05-15)**
- [x] **US-031: APP_INITIALIZER session bootstrap + errorInterceptor 401 fix + auth-guard.readme.md KT — PR #71 ✅ (2026-05-15)**
- [x] US-032: appsettings.Development.json — EP-016 closed ✅ (PR #54)
- [x] **US-035: System Context Diagram — EP-017 (#47, PR #53, Done)**
- [ ] **US-036: Container Diagram (C4 L2) — EP-017 (#48, trigger: end of Phase 2)**
- [ ] **US-037: Module Interaction Diagram — EP-017 (#49, trigger: end of Phase 2)**
- [ ] **US-038: Auth Flow Diagram — EP-017 (#50, trigger: end of Phase 2)**
- [ ] **US-039: Tenant Resolution Flow Diagram — EP-017 (#51, trigger: end of Phase 2)**
- [ ] **US-040: Deployment Diagram (Azure) — EP-017 (#52, trigger: end of Phase 7)**
- *(Old US-033 #36 closed — superseded by individual stories above)*

### Design System (established 2026-05-15, PR #64)
- Official palette: Indigo `#6366F1` / Purple `#8B5CF6` (Premium SaaS — Linear-inspired)
- `frontend/src/styles.scss` — full design system (1,161 lines): STRIDE CSS tokens, Tailwind `@theme inline` bridge, PrimeNG `--p-*` overrides, utility classes, animations
- `frontend/src/app/core/theme/theme.service.ts` — Signal-based ThemeService (light/dark, localStorage persist, OS-preference auto-detect)
- `frontend/src/index.html` — flash-of-wrong-theme prevention inline script
- `frontend/src/app/app.config.ts` — `providePrimeNG({ ripple: true, inputVariant: 'outlined' })`

### GitHub Board State (as of 2026-05-15)
- EP-001 through EP-016: ✅ Done (all closed)
- **EP-015 (Angular Login UI, #15): ✅ Done (closed) — US-034 ✅ US-030 ✅ US-031 ✅**
- EP-017 (Architecture Diagrams, #46): 🔵 Open — US-035 done (1/6); US-036–039 now unblocked (Phase 2 backend + UI complete)
- **Phase 2 is now functionally complete. US-036–039 architecture diagrams are next.**

## Phase 3 — Core Workflow Engine (**Complete** — 2026-05-16)
- EP-018 #72 (Domain Model): US-041 #77 ✅, US-042 #78 ✅, US-043 #79 ✅ — **Done** (PR #104, 2026-05-16)
  - 4 entity files: WorkflowDefinition, StepDefinition, WorkflowInstance, StepInstance
  - 12 domain events, 2 enums, WorkflowDomainException, domain.readme.md KT doc
- EP-019 #73 (Application Layer): US-044 #80 ✅, US-045 #81 ✅, US-046 #82 ✅, US-047 #83 ✅, US-048 #84 ✅ — **Done** (PR #105, 2026-05-16)
  - 2 repository abstractions (IWorkflowDefinitionRepository, IWorkflowInstanceRepository)
  - 12 commands + handlers + validators, 4 queries + handlers + DTOs + validators
  - LoggingBehaviour + ValidationBehaviour pipeline, application.readme.md KT doc
- EP-020 #74 (Infrastructure): US-049 #85 ✅, US-050 #86 ✅, US-051 #87 ✅ — **Done** (PR #106, 2026-05-16)
  - EF Core configs (4 tables, workflows schema, enum-as-string, indexes)
  - DomainEventNotification<T> + SaveChangesAsync domain event dispatch
  - WorkflowDefinitionRepository + WorkflowInstanceRepository (TenantAwareRepository)
  - IWorkflowReadService + WorkflowReadService (Dapper — list views + dashboard KPIs)
  - InitialCreate migration applied to local SQL Express
- EP-021 #75 (API Layer): US-052 #88 ✅, US-053 #89 ✅ — **Done** (PR #107, 2026-05-16)
  - WorkflowsController: 12 endpoints (definition CRUD + instance lifecycle)
  - StepsController: 4 step operation endpoints (assign/complete/fail/skip)
  - Request DTOs, `ITenantContext` injection fix (TenantId from context, not ICurrentUser), api.readme.md KT doc
- EP-022 #76 (Angular UI):
  - US-054 #90 ✅ — **Done** (PR #108, 2026-05-16) — Workflow list page: table/card view, filters, status badges, bulk-select, pagination, skeleton, empty state, WorkflowService, workflows-list.readme.md KT
  - US-055 #91 ✅ — **Done** (PR #110, 2026-05-16) — Workflow detail page: hero, step tracker, tabs, sidebar, actions, workflow-detail.readme.md KT
  - US-056 #92 ✅ — **Done** (PR #111, 2026-05-16) — Create + Edit workflow form (ReactiveFormsModule, dual-mode, step builder)
  - US-057 #93 ✅ — **Done** (PR #112, 2026-05-16) — StepActionModalComponent (assign/complete/fail/skip) + Run tab on detail page
- **EP-022 #76 ✅ COMPLETE** — All 4 stories done (US-054/055/056/057)
- **Issue #109** created: `[TASK] Wire BFF proxy routes for Workflow API` — medium priority, Phase 6

## Phase 4 — Dashboard & Reporting (**Complete** ✅ — 2026-05-19)
- EP-023 #94 ✅ **Done** (PR #132): SqlLoader, Dapper reporting DTOs, IReportingReadService
- EP-024 #95 ✅ **Done** (PR #133): Report domain entity, GenerateReport + ExportReportCsv handlers, WorkflowTrend handlers
- EP-025 #96 ✅ **Done** (PR #135): DashboardController, ReportsController (generate + export CSV)
- EP-026 #97 ✅ **Done** (PRs #136–#140):
  - US-062: DashboardPageComponent, KPI cards, p-chart trend+doughnut, ChartThemeService
  - BFF reporting proxy: `/bff/reporting/*` routes wired; ReportingApiClient typed HttpClient
  - BFF workflow proxy: `WorkflowApiClient` + BFF `WorkflowsController` — all `/bff/workflows/*` routes wired
  - Infra fixes (PR #138/#139): TenantMiddleware ordering, WorkflowDevDataSeeder, `MapInboundClaims = false`, `DateOnlyTypeHandler`
  - US-063: Angular Reporting page — generate/list/export reports, inline notifications, STRIDE button conventions
  - Bug fixes (PR #140): CSV export field name mismatch (`Id` → `ReportId`), bulk delete implemented, `JsonStringEnumConverter` global, Invalid Date fixes (`trendDate`, `createdAt`, `stepName`, `assigneeId`), dashboard moved to `features/dashboard/`

**All Phase 4 epics closed (EP-023/024/025/026). All stories CLOSED (#98–#103).**

## Phase 5 — Notifications & Observability (**Complete** ✅ — 2026-05-22)

### Completed
- [x] US-078 #146 ✅ — Notification entity + NotificationType enum + NotificationCreatedEvent + INotificationRepository (PR #158, 2026-05-22)
- [x] US-079 #147 ✅ — EF Core config + notifications schema migration + NotificationRepository (PR #158, 2026-05-22)
- [x] US-080 #148 ✅ — CreateNotificationCommand + MarkAsRead + Delete commands + handlers + validators (PR #159, 2026-05-22)
- [x] US-081 #149 ✅ — GetNotificationsQuery + GetUnreadCountQuery + handlers + validators (PR #159, 2026-05-22)
- [x] US-082 #150 ✅ — Workflow domain event handlers: WorkflowStarted/Completed/Failed, StepAssigned/Completed → auto-create notifications (PR #159, 2026-05-22)

### Completed (continued)
- [x] US-083 #151 ✅ — NotificationsController: GET list, GET unread-count, POST read, DELETE (PR #160, 2026-05-22)
- [x] US-084 #152 ✅ — NotificationsApiClient + BFF NotificationsController proxy `/bff/notifications/*` (PR #160, 2026-05-22)
- [x] US-085 #153 ✅ — Angular NotificationBellComponent (icon + unread count badge + dropdown panel, 60s polling) (PR #161, 2026-05-22)
- [x] US-086 #154 ✅ — Angular Notifications page (All/Unread tabs, skeleton, mark-as-read, dismiss, bulk mark-all-read) (PR #161, 2026-05-22)

- [x] US-087 #155 ✅ — SerilogEnrichmentMiddleware: UserId + TenantId on every log line (PR #162, 2026-05-22)
- [x] US-088 #156 ✅ — SQL Server + Redis health checks (ready tag) + UIResponseWriter verbose JSON (PR #162, 2026-05-22)
- [x] US-089 #157 ✅ — Angular /administration/health page: status cards, auto-refresh, BFF proxy (PR #162, 2026-05-22)

**EP-034 #145 ✅ COMPLETE** — All 3 stories done. Phase 5 Notifications & Observability fully complete.

## Phase 6 — SaaS Readiness (Planned 🔵 — 2026-05-22)

**Sprint Goal:** Activate the Administration and Invoicing stubs, deliver BYOT per-tenant customisation, build a self-service onboarding wizard, and harden the platform with rate limiting and an audit trail.

### Epics
| Epic | Scope | Stories | Status |
|---|---|---|---|
| EP-035 | Tenant User Administration (Administration module — list, invite, role, deactivate) | US-090–094 | [ ] Pending |
| EP-036 | Invoicing Core (Invoice aggregate → API → Angular list + KPI cards) | US-095–099 | [ ] Pending |
| EP-037 | Tenant Settings & BYOT (TenantSettings entity, per-tenant palette, CSS upload + sanitise + preview) | US-100–105 | [ ] Pending |
| EP-038 | Tenant Onboarding Flow (4-step wizard, RegisterTenantCommand, auto-login) | US-106–108 | [ ] Pending |
| EP-039 | SaaS Hardening (per-tenant rate limiting, AuditLog entity + trail, audit-log page) | US-109–111 | [ ] Pending |

**Architectural milestone unlocked:** Tenant onboarding flow complete (EP-038)
**Deferred stories resolved:** US-067–070 (BYOT, EP-027) land as US-103–105 in EP-037
**Note:** BFF Workflow proxy (#109) was completed in Phase 4 (PR #138) — not Phase 6 work.

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
