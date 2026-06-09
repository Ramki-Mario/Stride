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

## Phase 6 — SaaS Readiness (**✅ COMPLETE — 2026-05-30**)

**Sprint Goal:** Activate the Administration and Invoicing stubs, deliver BYOT per-tenant customisation, build a self-service onboarding wizard, and harden the platform with rate limiting and an audit trail.

### Epics
| Epic | GitHub # | Stories | PR | Status |
|---|---|---|---|---|
| EP-035 Tenant User Administration | #163 | US-090–094 (#168–172) | #190 ✅ | ✅ Done |
| EP-036 Invoicing Core | #164 | US-095–099 (#173–177) | — | ✅ Done |
| EP-037 Tenant Settings & BYOT | #165 | US-100–105 (#178–183) | — | ✅ Done |
| EP-038 Tenant Onboarding Flow | #166 | US-106–108 (#184–186) | — | ✅ Done |
| EP-039 SaaS Hardening | #167 | US-109–111 (#187–189) | — | ✅ Done — `cdba97f` |

**Architectural milestone unlocked:** Tenant onboarding flow complete (EP-038) ✅
**Deferred stories resolved:** US-067–070 (BYOT, EP-027) land as US-103–105 in EP-037 ✅
**Note:** BFF Workflow proxy (#109) was completed in Phase 4 (PR #138) — not Phase 6 work.

### Post-delivery bug fixes (2026-05-23)
| Bug | Fix | Commit |
|---|---|---|
| EF Core `InvalidIncludePathError` on invoice create — `.Include("_lineItems")` used private backing field | Changed to `.Include(i => i.LineItems)` strongly-typed lambda in `InvoiceRepository` | `fix(invoicing): fix EF Core Include path...` |
| Invoice total stuck at $0.00 — `computed()` doesn't track FormArray (RxJS) | Replaced with `signal<number>` + `lineItemsArray.valueChanges` subscription | same commit |
| Action menu dropdown invisible — `overflow: hidden` on `.inv-table-wrap` clipped it | Changed to `overflow: visible`, added corner `border-radius` on `<th>`/last `<td>` | `fix(invoicing): allow action menu to escape...` |
| Sidebar showing tenant name from wrong tenant (DisplayName backfill bug) | Added `else if (IsNullOrEmpty)` branch in `GetTenantSettingsQueryHandler` to backfill empty rows via `ITenantNameResolver` | prior session |
| `GET /bff/workflows/instances` 404 — all-instances endpoint missing from BFF and Host | Added `ListAllInstances` to Host controller + `GetAllInstancesAsync` to `WorkflowApiClient` + BFF proxy action | prior session |
| PrimeNG Select dropdown not styled | Updated global `styles.scss` with PrimeNG 21 selectors (`.p-select-overlay`, `data-p-focused`, `data-p-selected`) | prior session |
| BFF crash on startup — `InvalidOperationException: DefaultConnection is not configured` | `IDbConnectionFactory` was registered inside `AddBuildingBlocksInfrastructure` (shared by Host + BFF). Extracted to new `AddBuildingBlocksDatabase` extension called only from `STRIDE.Host/Program.cs` | `7c66271` |

### Infrastructure refactor (2026-05-23)
- **`IDbConnectionFactory`** introduced in `BuildingBlocks.Infrastructure.Persistence`
  - `SqlServerConnectionFactory` — live implementation
  - `MySqlConnectionFactory` — stub ready to uncomment (no NuGet change needed until switch)
  - All 7 Dapper services migrated off `new SqlConnection` / `IConfiguration` injection
- **`OpenAsync(ct)` + CancellationToken fixes** across 7 files:
  - `DeactivateUserAsync` / `ReactivateUserAsync` in `AdminWriteService` — CT was silently ignored during connection open
  - `WorkflowReadService` / `ReportingReadService` — CT was never passed to Dapper at all (used raw overloads without `CommandDefinition`)

### EP-039 — SaaS Hardening (✅ Done — 2026-05-30, commit `cdba97f`)
- **US-109** — `GlobalLimiter` in `Program.cs`: 100 req/60s per `tid` claim, falls back to remote IP for anonymous, `429 + Retry-After: 60`
- **US-110** — `AuditLog` immutable entity + `DapperAuditLogger` (fire-and-forget, `CancellationToken.None`), injected into 9 command handlers; `IAuditLogger` + `AuditActions` in `BuildingBlocks.Application.Abstractions`; EF migration `AddAuditLogTable` applied
- **US-111** — `AuditLogController` (`GET /api/administration/audit-log`, Admin role), BFF proxy, Angular `/administration/audit-log` page with date-range + action filters, expandable detail rows, skeleton, pagination; sidebar "Audit Log" link added

### Known deferred gaps (carry to Phase 7)
| Gap | Notes |
|---|---|
| Invite email + accept-invite flow | No email sent on invite; no token/link; no accept-invite page. `InviteUserCommandHandler` has `TODO` comment |
| Refresh token | `JwtTokenService` issues access token only; no refresh token entity, no `/auth/refresh` endpoint, no BFF auto-renew middleware |
| Tests | Zero test coverage across all modules — highest priority for Phase 7 |
| Deployment | No live URL yet — Phase 7 goal |

## Phase 7 — Portfolio & Deployment Polish (🔵 In Progress)

**Sprint Goal:** Close the test coverage gap, implement refresh tokens and invite email, build the Scheduling module, deliver the 14 Product Layer Gaps EPICs, and deploy a live dev environment.

### Milestone #10 — Core Hardening Epics (EP-040–044)

| Epic | GitHub # | Stories | Status |
|---|---|---|---|
| EP-040 Unit & Integration Tests | #195 | US-112–114 (#200–202) | ⏳ Pending |
| EP-041 Refresh Token Flow | #196 | US-115–118 (#203–206) | ⏳ Pending |
| EP-042 Invite Email Flow | #197 | US-119–122 (#207–210) | ⏳ Pending |
| EP-043 Scheduling Module | #198 | US-123–127 (#211–215) | ⏳ Pending |
| EP-044 Live Deployment | #199 | US-128–131 (#216–219) | ⏳ Pending |

### Milestone #11 — Product Layer Gaps (EP-048–061)

**Board status (2026-06-09):** EP-048–EP-054 ✅ Done. EP-055 Mobile-First 🔵 In Progress (US-168 ✅, US-169 🔵 PR #331 open, US-170 ✅ PR #332 merged). Extra Teams module (EP-062) delivered and closed. PRs #323 #324 #325 all merged.

**Epic → Issue → Story mapping — Milestone #11 original plan (✅ = Done, ⏳ = Next/Backlog):**
- EP-048 #263 → US-147–149 (#277–279) — Step Ownership & Assignment ✅
- EP-049 #264 → US-150–152 (#280–282) — Client/Customer Entity ✅
- EP-050 #265 → US-153–155 (#283–285) — Workflow ↔ Invoice Integration ✅
- EP-051 #266 → US-156–158 (#286–288) — File & Photo Attachments ✅
- EP-052 #267 → US-159–161 (#289–291) — Advanced Step Types ✅
- EP-053 #268 → US-162–164 (#292–294) — SLA & Deadline Tracking ✅
- EP-054 #269 → US-165–167 (#295–297) — Comments & Activity Log ✅ (PRs #320 #321 #322)
- EP-055 #270 → US-168–170 (#298–300) — Mobile-First Experience 🔵 In Progress (US-168 ✅ #298 closed, US-169 🔵 PR #331, US-170 ✅ #300 closed PR #332)
- EP-056 #271 → US-171–173 (#301–303) — Approval Gates ⏳
- EP-057 #272 → US-174–176 (#304–306) — Actionable Dashboard ⏳
- EP-058 #273 → US-177–179 (#307–309) — External Customer-Facing Link ⏳
- EP-059 #274 → US-180–182 (#310–312) — Webhooks and Integrations ⏳
- EP-060 #275 → US-183–185 (#313–315) — Operational Analytics ⏳
- EP-061 #276 → US-186 (#316) — README and Product Story ⏳

**Extra work delivered outside original plan:**
- EP-062 #327 → US-187 (#326) / US-188 (#328) / US-189 (#329) — Teams / Department Entity ✅ (PRs #323 #324 #325 — all closed, board=Done)

**Next:** EP-055 US-169 🔵 PR #331 pending merge (last story of EP-055). Then EP-056 Approval Gates (#271)

### EP-049 — Client/Customer Entity ✅ Done (#264, closed 2026-06-07)

#### US-150 — Client domain entity, repository, and CRUD API ✅ Done (commit `381fc14`)
- New `Clients` module: 4 src projects + 2 test projects added to solution
- `clients` SQL schema; `Client` aggregate (Name, ContactPerson, Email, Phone, Address, Notes, `ClientStatus` enum)
- `IClientRepository` + `ClientsDbContext` + `ClientConfiguration` (unique index filtered `[IsDeleted]=0`)
- Commands: CreateClient, UpdateClient, DeactivateClient, ReactivateClient (CQRS + MediatR)
- Queries: GetClientsQuery (paged, search, status filter), GetClientByIdQuery
- Embedded SQL: `GetClients.sql`, `GetClientById.sql` via Dapper
- `ClientsController` (Host) + `ClientsApiClient` (BFF typed HttpClient) + BFF `ClientsController`
- AuditActions: `client.created`, `client.updated`, `client.deactivated`, `client.reactivated`
- EF migration `20260607150131_CreateClientsSchema` applied locally
- 26 tests: 16 domain + 6 CreateClient handler + 4 UpdateClient handler — all green

#### US-151 — Client Management Screen (Angular) ✅ Done (commit `b82d9d0`)
- `/clients` lazy route + sidebar nav link (`pi-id-card`)
- KPI cards (total / active / inactive), debounced search, status filter dropdown
- Paginated data table with skeleton loader + empty + error states
- Create / Edit modal (pre-fills full detail including address + notes)
- 3-dot action menu: Edit, View History, Deactivate (Active) / Reactivate (Inactive)
- Backend: `ReactivateClientCommand` + handler; `PUT /api/clients/{id}/reactivate` on Host + BFF proxy

#### US-152 — Link workflow instances and invoices to a client ✅ Done (commit `56211b3`)
- Nullable `Guid? ClientId` FK on `WorkflowInstance` + `Invoice` domain entities (no EF nav property — cross-module)
- `StartWorkflowCommand` + `GenerateInvoiceCommand` accept optional `ClientId`; threaded through handlers + API
- EF configs: column + `(TenantId, ClientId)` index on both tables
- EF migrations `AddClientIdToWorkflowInstances` + `AddClientIdToInvoices` applied
- Clients module: `ClientHistoryDto`, `GetClientHistoryQuery/Handler`, `IClientReadService.GetClientHistoryAsync`
- Cross-schema Dapper SQL: `GetClientHistory_Workflows.sql` + `GetClientHistory_Invoices.sql`
- `GET /api/clients/{id}/history` Host endpoint + `ClientsApiClient.GetClientHistoryAsync` + BFF proxy
- Angular: `ClientHistoryDto`/`ClientWorkflowDto`/`ClientInvoiceDto` models; `getClientHistory()` in service
- Angular: "View History" action menu item → history modal (workflows table + invoices table, badge statuses, skeleton + error states)

---

### EP-050 — Workflow ↔ Invoice Integration 🔵 In Progress (#265)

#### US-153 — Billable items capture on workflow step completion ✅ Done (commit `fcf4728`)
- **Domain:** `BillableUnit` enum (Hours/Each/Day/Fixed), `BillableItem` aggregate child entity (validates description, qty > 0, price > 0; `LineTotal` computed); `StepInstance` extended with `TenantId`, `_billableItems` backing field, `BillableItems` read-only list; `StepInstance.Complete()` accepts optional `IReadOnlyList<(Description, Qty, Price, Unit)>`; `WorkflowInstance.CompleteStep()` passes through; `WorkflowInstance.Start()` passes `TenantId` to `StepInstance.Create()`
- **Infrastructure:** `BillableItemConfiguration` (table `BillableItems`, decimal(18,4), cascade delete); `StepInstanceConfiguration` updated (TenantId required, `HasMany` → `BillableItems` with backing field); `WorkflowsDbContext` registers `DbSet<BillableItem>`; `WorkflowInstanceRepository` eager-loads `.ThenInclude(s => s.BillableItems)`; EF migration `AddBillableItems` (`20260607174757`) applied
- **Application:** `CompleteStepCommand` extended with `IReadOnlyList<BillableItemInput>?`; handler maps inputs to tuple list and calls `instance.CompleteStep()`; `WorkflowInstanceDto` gets `BillableTotal` (sum across steps); `StepInstanceDto` gets `BillableSubtotal` + `BillableItems` list; `BillableItemDto` record; query handler maps `s.BillableItems`
- **API + BFF:** `CompleteStepRequest` + `BillableItemRequest` in `StepRequests.cs`; `StepsController.Complete()` accepts body, maps to `BillableItemInput[]`; `WorkflowApiClient.CompleteStepAsync()` forwards nullable body; BFF `WorkflowsController.CompleteStep()` reads and forwards body stream transparently
- **Angular:** `BillableUnit` type + `BILLABLE_UNIT_LABELS`; `BillableItemDto`/`BillableItemInput` models; `WorkflowService.completeStep()` sends `{ billableItems }` if any; `step-action-modal` billable widget (FormArray, add/remove rows, live running total, validation); `workflow-detail-page` shows per-step subtotal toggle + expandable line-items table, instance-level billable grand total
- **Tests:** `BillableItemTests.cs` (9 domain tests — valid items, multiple items, empty, empty/zero/negative validation), `CompleteStepCommandHandlerTests.cs` (5 handler tests — not found, no items, with items, persists, domain exception); `InternalsVisibleTo` added to Workflows.Application.csproj; Application.Tests Usings.cs updated (FluentAssertions + NSubstitute)
- Issue #283 closed; board item → Done

#### US-154 — Auto invoice draft on workflow instance closure ✅ Done (commit `84c3533`)
- **Domain enrichment:** `WorkflowBillableItemSnapshot` record added to Workflows.Domain/Events; `WorkflowCompletedEvent` extended with `WorkflowName`, `ClientId`, `BillableItems` (all defaulted so existing code compiles); `WorkflowInstance.CheckCompletion()` builds snapshot and passes all 6 args
- **Invoice domain:** `ClientEmail` made nullable (`string?`) at `Generate()` time — email now only enforced at `Send()` time (guard added to `Send()`); `SourceWorkflowInstanceId: Guid?` property added; `NewInvoice` record updated with both nullable fields
- **Invoicing.Infrastructure:** `InvoiceConfiguration` — removed `.IsRequired()` from ClientEmail, added `SourceWorkflowInstanceId` property + index on `(TenantId, SourceWorkflowInstanceId)`; `InvoiceRepository` implements `GetBySourceWorkflowInstanceIdAsync`; EF migration `20260607181957_AddSourceWorkflowInstanceIdToInvoices` applied
- **Invoicing.Application:** `CreateInvoiceDraftFromWorkflowHandler` — idempotent draft creation; decimal qty preserved as `(qty=1, unitPrice=lineTotal)`; description embeds `"{desc} ({qty} × {unit} @ £{price})"`. Handles `DomainEventNotification<WorkflowCompletedEvent>`. Failures swallowed (logs error, never re-throws)
- **Notifications.Application:** `InvoiceDraftCreatedNotificationHandler` — sends `NotificationType.InvoiceDraftCreated` to `StartedBy` on same event; Notifications.Domain gains `InvoiceDraftCreated` enum value
- **Invoicing.Application.Tests.csproj:** Added project references to `BuildingBlocks.Infrastructure` + `Workflows.Domain`; Usings.cs updated with FluentAssertions + NSubstitute global usings
- **Tests:** 7 new tests in `CreateInvoiceDraftFromWorkflowHandlerTests.cs` (persists, invoice number, sourceId, billable items, no items, clientId, idempotency); domain test updated: removed empty-email-at-generate case, added `Send_WhenClientEmailMissing` test; all 108 tests green
- Issue #284 closed; board item → Done

#### US-155 — Invoice status badge on workflow instance detail ✅ Done (commit `b1b3e45`)
- **Backend:** `GET /api/invoicing/invoices/by-workflow/{id}` → `InvoiceReferenceDto` (id, number, status, label) or 404; `POST /api/invoicing/invoices/from-workflow/{id}` → idempotent manual draft creation (accepts workflowName, clientId, billableItems from frontend); `InvoiceDetailDto` + `InvoiceSummaryDto`: `ClientEmail` → `string?`; `SourceWorkflowInstanceId: Guid?` added to `InvoiceDetailDto`; `GetInvoiceById.sql` updated; `InvoiceReadService` maps both new fields
- **BFF:** both new endpoints proxied via `InvoicingApiClient` + `InvoicingController`
- **Angular:** `InvoiceReferenceDto` + `INVOICE_STATUS_CSS` + `CreateWorkflowInvoiceRequest` added to models; `InvoiceService` gets `getInvoiceByWorkflowInstanceId()` / `createInvoiceFromWorkflow()`; workflow detail page invoice panel (badge, View Invoice link, Create Invoice Draft button) on completed instances; new `InvoiceDetailPageComponent` at `/invoicing/:id` with status management + source-workflow reference card; route added to `invoicing.routes.ts`
- **Tests:** 4 new tests for `GetInvoiceByWorkflowInstanceIdQueryHandler`; 112 tests green
- Issue #285 closed; board item → Done

---

### EP-055 — Mobile-First Experience 🔵 In Progress (#270)

**GitHub:** Epic #270 | Stories: US-168 #298 ✅ / US-169 #299 🔵 PR #331 open / US-170 #300 ✅ PR #332 merged

#### US-168 — Responsive layout refactor ✅ Done (PR #330, GitHub #298 closed, board=Done)
- Shell: `mobileNavOpen` signal; sidebar fixed off-canvas overlay at <640px, slides in from left; backdrop closes on tap
- Topbar: hamburger button (44px touch target, mobile-only CSS); hides username, breadcrumb root, palette switcher at <640px
- Sidebar: `mobileOpen` `@Input`, `navClose` `@Output`, `.mobile-open` host class, nav-item click emits close, hides collapse btn on mobile
- Global `styles.scss`: off-canvas sidebar rules, 44×44px touch target minimums (`.stride-btn*`, topbar action buttons)
- Dashboard page: reduced padding + chart height at <640px
- Workflows list: touch targets, full-width search, stacked toolbar at <640px
- My Tasks: padding reduction + touch target for card button
- Workflow Instances: progress-bar column hidden at <640px; padding reduction; touch targets
- Workflow Detail: step-action and hero button touch targets; breadcrumb wraps gracefully on mobile
- **341 backend tests pass** ✅ · dev build clean ✅

#### US-169 — Mobile-optimised step completion with camera capture 🔵 In Review (PR #331, GitHub #299 board=In Progress)
- step-action-modal: bottom-sheet on mobile (<640px) — slides up from bottom, full width, 92dvh max, rounded top corners
- step-action-modal: **Photos** section in complete action — Camera button (`accept="image/*" capture="environment"`) opens rear camera; Gallery button opens file picker; thumbnails with remove; uploads fire after step completes (non-blocking)
- step-action-modal: billable items grid collapses 6-col → 2-col stacked on mobile; footer buttons go full-width 2.75rem
- my-tasks-page: quick-complete ✓ button on Pending/InProgress tasks — always-visible on mobile (2.75rem), hover-visible on desktop; calls `completeStep` with no fields/billables, reloads list
- **dev build clean** ✅

#### US-170 — PWA setup (installable app, service worker, offline queue, Web Push) ✅ Done (PR #332 merged, GitHub #300 closed, board=Done)
- `ngsw-config.json`: Angular SW app-shell prefetch + lazy assets caching; `serviceWorker` enabled in production `angular.json`
- `manifest.webmanifest`: `display: standalone`, `theme_color: #B97AF9`, PNG icons (192/512), SVG icon, apple-touch-icon, My Tasks shortcut
- `index.html`: manifest link, theme-color, apple-mobile-web-app meta tags
- `ConnectivityService`: online/offline signal via `window` online/offline events
- `OfflineQueueService`: IndexedDB store `stride-offline`/`step-completions` — `enqueue`, `dequeue`, `getAll`, `count`
- `SyncService`: drains IndexedDB on `online` event using `fetch` with `credentials: include`; `pendingCount`, `isSyncing`, `lastError` signals
- `InstallPromptService`: deferred `beforeinstallprompt` (Android native) + iOS `/iphone|ipad|ipod/` detection; `shouldShowBanner` getter
- `PushNotificationService`: `SwPush.requestSubscription` → POST `/bff/notifications/push/subscribe`; `unsubscribe` removes endpoint
- `WorkflowService.completeStep`: enqueues to IndexedDB when offline; resolves immediately
- `ShellComponent`: offline banner, sync progress banner, sync error banner (with dismiss), Android install banner, iOS install instruction banner; drains queue on `ngOnInit` if online
- `styles.scss`: PWA banner styles (offline amber, sync blue, error red, install)
- Backend — Notifications module:
  - `PushSubscription` entity (already existed from previous session) + `PushSubscriptionConfiguration` EF config
  - Migration `20260609000000_AddPushSubscriptionsTable` (written manually — Host lacks EF Design pkg)
  - `IPushSubscriptionRepository` + `PushSubscriptionRepository`
  - `RegisterPushSubscriptionCommand` / `UnregisterPushSubscriptionCommand` (CQRS + validator)
  - `GetVapidPublicKeyQuery` reads `Vapid:PublicKey` from config
  - `IWebPushService` / `WebPushService` using `Lib.Net.Http.WebPush` + VAPID auth
  - `StepAssignedNotificationHandler`: now also sends Web Push to all user subscriptions
  - Host `PushController` (`GET vapid-key` [AllowAnonymous], `POST subscribe`, `POST unsubscribe`)
  - BFF `NotificationsApiClient` extended; BFF `NotificationsController` adds push proxy endpoints
  - `appsettings.json`: `Vapid.Subject` + `Vapid.PublicKey`; private key in gitignored `appsettings.Development.json`
- **Backend + frontend builds clean** ✅ (only pre-existing Sass @import deprecation warning)

---

### EP-062 — Teams / Department Entity ✅ Done (extra epic, 2026-06-09)

**GitHub:** Epic #327 (closed) | Stories: US-187 #326 ✅ / US-188 #328 ✅ / US-189 #329 ✅ | All board=Done

#### US-187 — Teams module scaffold, CQRS, Dapper read service ✅ Done (commit `532e583`+`9e9a178`, PR #323, GitHub #326 ✅ closed)
- New `STRIDE.Modules.Teams` module: 4 src projects + 2 test projects (Domain, Application, Infrastructure, API + Domain.Tests, Application.Tests + Infrastructure.Tests)
- `Team` aggregate: Name, Description, `ParentTeamId` self-referential FK, `TeamStatus` (Active=0/Inactive=1), soft-delete
- Full CQRS: CreateTeam, UpdateTeam, DeactivateTeam, ReactivateTeam commands; GetTeams (paged/search/filter), GetTeamById queries
- Dapper read service with embedded SQL; EF migration `CreateTeamsSchema` (schema `teams`, unique filtered index, `DeleteBehavior.NoAction` for self-ref FK)
- BFF: `TeamsApiClient` + BFF `TeamsController` wired to `/bff/teams`; AuditActions extended
- 341 total tests (76 Teams: 26 domain + 50 app; SonarCloud quality gate ≥80% ✅)
- Coverage fix: `TeamRepositoryTests` (9 EF InMemory), `InternalsVisibleTo` added, `**/ReadModels/**` excluded from `sonar.coverage.exclusions`

#### US-188 — Team ↔ Workflow assignment ✅ Done (commit `e6bb5ce`, PR #324, GitHub #328 ✅ closed)
- Nullable `TeamId` FK on `WorkflowInstance` (no EF nav, bare column + composite index, SQL LEFT JOIN for name)
- `AssignTeamToWorkflow` command + handler; filter instances by team; `WorkflowTeamAssignedEvent`
- `GetWorkflowInstanceSummariesByTeam.sql`; Angular team picker in workflow-detail; 6 domain + 5 app tests

#### US-189 — Teams Angular management page ✅ Done (commit `11c2159`, PR #325, GitHub #329 ✅ closed)
- `/teams` lazy route + sidebar nav link; KPI cards, debounced search, status filter, pagination
- Create/Edit modal: name, description, parent team picker (excludes self); row actions: Edit, Deactivate, Reactivate
- `TeamsService` full CRUD signals; `PagedResult<T>.items` mapping fix; numeric `TeamStatus` model fix

---

### ⚠️ GitHub Issue Hierarchy Process (MANDATORY for every sprint)
When creating issues for a new phase/sprint, always:
1. Create Epic issues → assign milestone → label `phase-N` + `epic`
2. Create US issues → assign same milestone → label `phase-N` + `user-story`
3. Link each US as a sub-issue of its Epic immediately after creation:
```powershell
$childId = (gh api repos/Ramki-Mario/Stride/issues/{child_number} | ConvertFrom-Json).id
gh api --method POST repos/Ramki-Mario/Stride/issues/{epic_number}/sub_issues --field "sub_issue_id=$childId"
```
4. Add all issues to project board + set Status = Backlog
API note: uses integer `.id` (not `nodeId`, not issue `number`). 422 = already linked (safe to ignore).

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
