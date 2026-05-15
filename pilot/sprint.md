# STRIDE — Sprint Planning

> Enterprise-style sprint plan. Updated at the end of every implementation sprint.
> Align with: implementation-roadmap.md, architecture.md, discussions.md, current-status.md

---

## Sprint Conventions

| Field | Convention |
|---|---|
| Epic prefix | `EP-` |
| Story prefix | `US-` |
| Task prefix | `T-` |
| Complexity | XS (0.5d) / S (1d) / M (2d) / L (3d) / XL (5d+) |
| Status | `[ ]` Pending / `[x]` Done / `[-]` In Progress / `[~]` Deferred |

---

## Sprint 1 — Monorepo & Foundation (COMPLETE)

**Sprint Goal:** Establish the engineering monorepo, backend solution, frontend workspace, Docker foundation, and all cross-cutting infrastructure so that future feature sprints can build on a stable, tenant-aware scaffold.

**Dates:** 2026-05-14 (single session, AI-assisted)
**Phase:** Phase 1

---

### EP-001 — Monorepo Structure

**Goal:** Single-repo folder hierarchy that scales to 50+ projects without friction.

**Acceptance Criteria:**
- `/backend`, `/frontend`, `/infrastructure`, `/docker`, `/docs`, `/scripts` exist
- `.gitignore` excludes all build artifacts, secrets, and OS detritus
- `.gitattributes` normalizes line endings; CRLF enforced for `.sln`/`.csproj`
- `global.json` pins .NET SDK version; prevents accidental toolchain drift

| ID | Story | Tasks | Complexity | Status |
|---|---|---|---|---|
| US-001 | As a developer, I can clone the repo and have a predictable folder layout | T-001 Create folder hierarchy<br>T-002 Write .gitignore<br>T-003 Write .gitattributes<br>T-004 Write global.json | S | [x] |
| US-002 | As a developer, I can see all required environment variables documented | T-005 Create .env.example with all variables | XS | [x] |

**Dependencies:** None
**Risks:** None

---

### EP-002 — Toolchain Baseline

**Goal:** Pin exact toolchain versions to guarantee reproducible builds across machines.

**Acceptance Criteria:**
- .NET SDK 10.0.300 installed and verified (`dotnet --version`)
- Node.js 26.1.0 installed (`node --version`)
- npm 11.14.1 installed (`npm --version`)
- Angular CLI 21.2.11 installed globally (`ng version`)

| ID | Story | Tasks | Complexity | Status |
|---|---|---|---|---|
| US-003 | As a developer, I build the backend with .NET 10 SDK | T-006 Install .NET SDK 10.0.300<br>T-007 Verify global.json locks SDK | XS | [x] |
| US-004 | As a developer, I run the Angular frontend with Node 26 | T-008 Install Node.js 26.1.0<br>T-009 Update npm to 11.14.1<br>T-010 Install Angular CLI 21.2.11 | S | [x] |

**Dependencies:** EP-001
**Risks:** Angular CLI 21 flags Node 26 as "unsupported" (warning only — build succeeds)

---

### EP-003 — Backend Solution Scaffold

**Goal:** 50-project .NET 10 solution with full modular monolith hierarchy, building cleanly.

**Acceptance Criteria:**
- `dotnet build` succeeds with zero errors across all 50 projects
- Solution folder structure: BuildingBlocks / Modules / Host / BFF / Gateway / Tests
- All 7 modules each have 4 layers: Domain, Application, Infrastructure, API
- Each module API project registers via a single `Add{Module}Module()` extension method
- All module DbContexts use SQL schema separation via `HasDefaultSchema("...")`

| ID | Story | Tasks | Complexity | Status |
|---|---|---|---|---|
| US-005 | As a developer, I open STRIDE.sln and see all 50 projects organized in solution folders | T-011 Create STRIDE.sln<br>T-012 Create all 50 .csproj files<br>T-013 Register all projects in solution folders | M | [x] |
| US-006 | As a developer, BuildingBlocks provides stable domain/app/infra abstractions | T-014 Implement BuildingBlocks.Domain (IDomainEvent, BaseEntity, AuditableEntity, ValueObject)<br>T-015 Implement BuildingBlocks.Application (IIntegrationEvent, IEventBus, IBackgroundTaskQueue, ITenantContext, ICurrentUser, Result\<T\>, LoggingBehaviour, ValidationBehaviour)<br>T-016 Implement BuildingBlocks.Infrastructure (MediatREventBus, TenantContextProvider, TenantMiddleware, TenantAwareRepository, CorrelationIdMiddleware, InfrastructureServiceExtensions) | L | [x] |
| US-007 | As a developer, every module has a clean 4-layer skeleton | T-017 Scaffold Identity (Domain/App/Infra/API)<br>T-018 Scaffold Workflows (Domain/App/Infra/API)<br>T-019 Scaffold Scheduling (Domain/App/Infra/API)<br>T-020 Scaffold Reporting (Domain/App/Infra/API)<br>T-021 Scaffold Notifications (Domain/App/Infra/API)<br>T-022 Scaffold Invoicing (Domain/App/Infra/API)<br>T-023 Scaffold Administration (Domain/App/Infra/API) | XL | [x] |
| US-008 | As a developer, STRIDE.Host composes all modules and starts cleanly | T-024 Implement Program.cs with all 7 module registrations<br>T-025 Configure middleware pipeline (Serilog, CorrelationId, TenantMiddleware)<br>T-026 Wire health endpoints (/health, /health/live, /health/ready)<br>T-027 Write appsettings.json | M | [x] |
| US-009 | As a developer, STRIDE.BFF provides typed HTTP forwarding to Host | T-028 Implement BFF Program.cs (HttpClient, CORS, session)<br>T-029 Scaffold IdentityApiClient typed client<br>T-030 Write BFF appsettings.json | S | [x] |

**Dependencies:** EP-001, EP-002
**Risks:**
- `IIntegrationEvent : INotification` must live in Application (not Domain) — ADR-014 [RESOLVED]
- API layer must reference Infrastructure for DI composition — ADR boundary clarified [RESOLVED]
- EF Core + SqlServer package pinned to 9.0.5 for .NET 10 compatibility [RESOLVED]

---

### EP-004 — Frontend Scaffold

**Goal:** Angular 21 SPA with Tailwind 4, PrimeNG 21, standalone components, feature-based routing.

**Acceptance Criteria:**
- `ng build --configuration=development` compiles with zero errors
- Tailwind 4 CSS-first (`@import "tailwindcss"`) in styles.scss — no config file
- PrimeNG 21 + PrimeIcons installed
- Standalone components only — no NgModules
- All 7 feature routes lazy-loaded from shell
- AuthService uses Signals (`signal<AuthUser | null>`)
- HTTP interceptors: correlation ID, error (401/403 handling)

| ID | Story | Tasks | Complexity | Status |
|---|---|---|---|---|
| US-010 | As a developer, the Angular workspace starts cleanly with all dependencies installed | T-031 Initialize Angular 21 workspace (standalone)<br>T-032 Install Tailwind CSS 4<br>T-033 Install PrimeNG 21 + PrimeIcons 7<br>T-034 Install @angular/animations | S | [x] |
| US-011 | As a developer, the app bootstraps with correct providers | T-035 Write app.config.ts (Router, HttpClient, Animations)<br>T-036 Write app.routes.ts (authGuard at root, 7 lazy routes)<br>T-037 Write app.ts (bootstrapApplication) | S | [x] |
| US-012 | As a user (future), I am protected by an auth guard that checks the BFF session | T-038 Implement AuthService (Signals, BFF /auth/me calls)<br>T-039 Implement TenantService (computed from AuthService)<br>T-040 Implement authGuard (functional CanActivateFn)<br>T-041 Implement correlationInterceptor<br>T-042 Implement errorInterceptor | M | [x] |
| US-013 | As a developer, all 7 feature modules have placeholder pages and lazy routes | T-043 Create feature directory structure (7 features × 5 subdirs)<br>T-044 Create feature routes files<br>T-045 Create placeholder page components | S | [x] |
| US-014 | As a developer, the layout shell renders sidebar and topbar stubs | T-046 Implement ShellComponent<br>T-047 Implement SidebarComponent stub<br>T-048 Implement TopbarComponent stub | S | [x] |

**Dependencies:** EP-002
**Risks:**
- Tailwind 4 has no config file — Angular SASS plugin warns but does not fail [MITIGATED]
- Angular CLI 21 + Node 26 compatibility warning (warning only) [MITIGATED]

---

### EP-005 — Docker & Infrastructure Foundation

**Goal:** Full Docker Compose environment with all supporting services health-checked and networked.

**Acceptance Criteria:**
- `docker compose up` starts: stride-host, stride-bff, sqlserver, redis, seq
- All services have health checks
- Named volumes for sqlserver and seq data
- Dockerfiles target .NET 10 runtime image

| ID | Story | Tasks | Complexity | Status |
|---|---|---|---|---|
| US-015 | As a developer, I spin up the full stack with a single docker compose command | T-049 Write docker-compose.yml (5 services, health checks, volumes)<br>T-050 Write docker/stride-host/Dockerfile<br>T-051 Write docker/stride-bff/Dockerfile | M | [x] |

**Dependencies:** EP-003
**Risks:** SQL Server 2022 image requires 4GB RAM minimum on host

---

### EP-006 — CI/CD Skeleton

**Goal:** GitHub Actions workflow that builds and tests both backend and frontend on every push.

**Acceptance Criteria:**
- `.github/workflows/ci.yml` exists
- Backend job: dotnet restore → build → test
- Frontend job: npm ci → ng build --production
- No deploy steps yet (Phase 7)

| ID | Story | Tasks | Complexity | Status |
|---|---|---|---|---|
| US-016 | As a developer, pushes to any branch trigger CI builds | T-052 Write ci.yml (backend + frontend jobs)<br>T-053 Configure .NET 10 action<br>T-054 Configure Node 22 action | S | [x] |

**Dependencies:** EP-003, EP-004
**Risks:** None

---

### EP-007 — Source Control & Repository

**Goal:** GitHub repository created, first commit pushed, team can clone.

**Acceptance Criteria:**
- Public GitHub repo `Ramki-Mario/Stride` exists
- First commit includes all 234 files (Phase 1 deliverables)
- `node_modules`, `bin`, `obj`, `dist` excluded by .gitignore

| ID | Story | Tasks | Complexity | Status |
|---|---|---|---|---|
| US-017 | As a team member, I can clone the repo and build immediately | T-055 git init + configure user<br>T-056 gh repo create Stride<br>T-057 Stage and push first commit | XS | [x] |

**Dependencies:** EP-001 through EP-006
**Risks:** None

---

### Sprint 1 Architectural Milestones

| Milestone | Status |
|---|---|
| Modular monolith folder hierarchy established | [x] |
| BuildingBlocks.Domain has zero external dependencies | [x] |
| IIntegrationEvent correctly placed in Application layer | [x] |
| TenantAwareRepository auto-applies TenantId + IsDeleted filters | [x] |
| All module DbContexts use SQL schema separation | [x] |
| Angular SPA uses standalone components only | [x] |
| AuthService state managed via Signals | [x] |
| Frontend holds no tokens (BFF-only pattern scaffolded) | [x] |
| All 50 projects build on .NET 10 | [x] |
| CI pipeline validates both backend and frontend | [x] |

---

### Sprint 1 Cross-Cutting Concerns Resolved

| Concern | Strategy | Location |
|---|---|---|
| Tenant isolation | `TenantAwareRepository` filters `TenantId + IsDeleted` | BuildingBlocks.Infrastructure |
| Correlation tracking | `CorrelationIdMiddleware` + `X-Correlation-Id` header | BuildingBlocks.Infrastructure |
| Structured logging | Serilog with Seq sink; TenantId/CorrelationId in scope | STRIDE.Host appsettings |
| Cancellation propagation | All async I/O passes `CancellationToken` | Enforced by coding standards |
| Validation | FluentValidation via `ValidationBehaviour` MediatR pipeline | BuildingBlocks.Application |
| Error result wrapping | Custom `Result<T>` — no external NuGet | BuildingBlocks.Application |
| SPA auth token safety | BFF issues HttpOnly cookies; Angular holds no tokens | Scaffolded in BFF + AuthService |

---

## Sprint 2 — Identity & Tenant Foundation (NEXT)

**Sprint Goal:** Implement secure, tenant-aware authentication and authorization foundation. After this sprint, a user can log in, be resolved to a tenant, receive an HttpOnly session cookie backed by Redis, and be authorized via claims-based RBAC — all without any token ever reaching the browser.

**Phase:** Phase 2
**Status:** IN PROGRESS (US-018 through US-022 complete)

---

### EP-008 — Identity Domain Model

**Goal:** Define the core identity domain entities: User, Role, Permission.

**Acceptance Criteria:**
- `User` extends `AuditableEntity` — has Email, PasswordHash, DisplayName, IsActive
- `Role` entity with Name, NormalizedName, Description
- `Permission` entity with Name, Resource, Action
- `UserRole` join (many-to-many, soft-deleteable)
- `RolePermission` join (many-to-many)
- All entities are tenant-scoped (TenantId on AuditableEntity)
- Domain events: `UserCreatedEvent`, `UserRoleAssignedEvent`
- No EF Core leakage into Domain layer

| ID | Story | Tasks | Complexity | Status |
|---|---|---|---|---|
| US-018 | As a tenant admin, I can manage users with roles and permissions | T-058 Implement User entity + domain events<br>T-059 Implement Role entity<br>T-060 Implement Permission entity<br>T-061 Implement UserRole join entity<br>T-062 Implement RolePermission join entity | M | [x] |
| US-019 | As an engineer, domain logic for password hashing lives in the domain, not infrastructure | T-063 Implement Password value object (hashing abstraction) | S | [x] |

**Dependencies:** BuildingBlocks.Domain (EP-003 — complete)
**Risks:** Password hashing — use `IPasswordHasher<T>` abstraction in Application; inject BCrypt/ASP.NET Core Identity hasher in Infrastructure

---

### EP-009 — Tenant Domain Model

**Goal:** Tenant entity hierarchy enabling corporate domain-based and generic domain-based tenant resolution.

**Acceptance Criteria:**
- `Tenant` entity: Name, Slug, IsActive, Plan
- `TenantDomainMapping`: TenantId + CorporateDomain (e.g. `acme.com`)
- `UserTenantMapping`: UserId + TenantId + Role (for generic-domain users)
- Unique constraint on corporate domain
- Tenant resolution strategy:
  - email domain in `TenantDomainMapping` → resolve to that Tenant
  - email domain not found → look up `UserTenantMapping` for explicit assignment
- Soft-delete on all three entities

| ID | Story | Tasks | Complexity | Status |
|---|---|---|---|---|
| US-020 | As a SaaS operator, each tenant maps to a corporate email domain | T-064 Implement Tenant entity<br>T-065 Implement TenantDomainMapping entity<br>T-066 Implement UserTenantMapping entity | S | [x] |
| US-021 | As a user with a generic email (e.g. gmail), I am resolved to a tenant via explicit assignment | T-067 Implement TenantResolver (corporate domain → UserTenantMapping fallback) | M | [x] |

**Dependencies:** EP-008
**Risks:** Tenant resolver must be called before `TenantMiddleware` sets TenantId on the context — ordering matters in BFF login flow

---

### EP-010 — Identity Infrastructure (EF Core + Migrations)

**Goal:** EF Core entity configurations, migration baseline, and repository implementations for Identity module.

**Acceptance Criteria:**
- `IdentityDbContext` uses schema `identity`
- EF configurations for User, Role, Permission, UserRole, RolePermission, Tenant, TenantDomainMapping, UserTenantMapping
- Initial migration generated and applied
- `IUserRepository`, `IRoleRepository`, `ITenantRepository` interfaces in Application
- Implementations in Infrastructure via `TenantAwareRepository<,>`

| ID | Story | Tasks | Complexity | Status |
|---|---|---|---|---|
| US-022 | As a developer, I can run EF migrations to create the identity schema | T-068 Write EF configurations for all 8 entities<br>T-069 Generate initial migration<br>T-070 Seed default roles (Admin, Member, Viewer) | M | [x] |
| US-023 | As a developer, all identity queries are tenant-scoped automatically | T-071 Implement UserRepository : TenantAwareRepository\<User, IdentityDbContext\><br>T-072 Implement RoleRepository<br>T-073 Implement TenantRepository | S | [x] |

**Dependencies:** EP-009
**Risks:** `dotnet ef migrations add` requires Design package — already added in Phase 1

---

### EP-011 — JWT Strategy & Internal Auth Provider

**Goal:** Issue signed JWTs internally (no external IdP yet); BFF exchanges JWT for HttpOnly session cookie backed by Redis.

**Acceptance Criteria:**
- `IJwtTokenService` in Application layer (interface only)
- `JwtTokenService` in Infrastructure — signs JWT with `HS256`, configurable secret + expiry
- JWT claims: `sub` (UserId), `email`, `tid` (TenantId), `roles[]`
- BFF login: receives JWT from Host Identity API → stores session in Redis → issues HttpOnly cookie to browser
- Redis key pattern: `tenant:{tenantId}:session:{sessionId}`
- Cookie: HttpOnly, SameSite=Strict, Secure in production
- No JWT ever reaches the browser

| ID | Story | Tasks | Complexity | Status |
|---|---|---|---|---|
| US-024 | As a user, logging in returns an HttpOnly cookie — no token in the browser | T-074 Implement IJwtTokenService + JwtTokenService<br>T-075 Implement ISessionStore (Redis-backed)<br>T-076 Implement BFF LoginEndpoint (POST /bff/auth/login)<br>T-077 Implement BFF LogoutEndpoint (POST /bff/auth/logout)<br>T-078 Implement BFF MeEndpoint (GET /bff/auth/me) | L | [x] |
| US-025 | As a developer, JWT configuration is environment-driven | T-079 Add Jwt:Secret, Jwt:Issuer, Jwt:ExpiryMinutes to appsettings + .env.example | XS | [ ] |

**Dependencies:** EP-010
**Risks:**
- Redis session store must be registered before cookie middleware in BFF pipeline
- `ISessionStore` must namespace keys per tenant to prevent cross-tenant session bleed

---

### EP-012 — Identity Application Layer (Commands & Queries)

**Goal:** MediatR handlers for login, registration, role assignment, and user queries.

**Acceptance Criteria:**
- Commands: `LoginCommand`, `RegisterUserCommand`, `AssignRoleCommand`
- Queries: `GetUserByIdQuery`, `GetUserByEmailQuery`, `GetUsersQuery`
- All handlers return `Result<T>` — never throw for domain failures
- `LoginCommand` validates credentials, resolves tenant, returns JWT payload
- All handlers are tenant-scoped via `ITenantContext`

| ID | Story | Tasks | Complexity | Status |
|---|---|---|---|---|
| US-026 | As a developer, all identity operations flow through MediatR handlers | T-080 Implement LoginCommand + handler<br>T-081 Implement RegisterUserCommand + handler<br>T-082 Implement AssignRoleCommand + handler<br>T-083 Implement GetUserByIdQuery + handler<br>T-084 Implement GetUserByEmailQuery + handler | L | [ ] |
| US-027 | As an operator, invalid login attempts return structured errors, not exceptions | T-085 Add FluentValidation validators for LoginCommand + RegisterUserCommand | S | [ ] |

**Dependencies:** EP-010, EP-011
**Risks:** LoginCommand must not set TenantId on context before resolving it — login is the resolution step

---

### EP-013 — Identity API Layer

**Goal:** Thin controllers that dispatch to MediatR and return HTTP results.

**Acceptance Criteria:**
- `POST /api/identity/users/register` → `RegisterUserCommand`
- `POST /api/identity/auth/login` → `LoginCommand`
- `POST /api/identity/users/{id}/roles` → `AssignRoleCommand`
- `GET /api/identity/users/{id}` → `GetUserByIdQuery`
- All endpoints return `ProblemDetails` on failure (using `Result<T>` → HTTP mapping)
- Controllers inject `IMediator` only

| ID | Story | Tasks | Complexity | Status |
|---|---|---|---|---|
| US-028 | As a developer, Identity API endpoints are thin, MediatR-dispatching controllers | T-086 Implement UsersController<br>T-087 Implement AuthController (internal, called by BFF only)<br>T-088 Implement Result → IActionResult mapping helper | M | [ ] |

**Dependencies:** EP-012
**Risks:** AuthController should not be publicly routable — internal only (Host binds to internal Docker network port)

---

### EP-014 — RBAC & Claims Authorization

**Goal:** Claims-based authorization wired into ASP.NET Core; permission-based policies available for module controllers.

**Acceptance Criteria:**
- `[Authorize(Policy = "CanManageUsers")]` pattern works
- Permissions seeded: `users.create`, `users.read`, `users.manage`, `workflows.create`, etc.
- `IAuthorizationPolicyProvider` or conventional policy registration
- Claims populated from JWT: `roles[]` array claim
- Role hierarchy: Admin > Member > Viewer

| ID | Story | Tasks | Complexity | Status |
|---|---|---|---|---|
| US-029 | As a developer, I can protect any endpoint with a permission policy in one attribute | T-089 Define permission constants<br>T-090 Register authorization policies in Host<br>T-091 Implement PermissionAuthorizationHandler | M | [ ] |

**Dependencies:** EP-012
**Risks:** Policy registration must be done in Host, not per-module — policies are a cross-cutting concern

---

### EP-015 — Angular Login UI

**Goal:** Functional login page with reactive form, PrimeNG components, wired to AuthService → BFF.

**Acceptance Criteria:**
- Login page: email + password fields, submit button, error display
- Uses PrimeNG `InputText`, `Password`, `Button`
- Reactive form with validators (required, email format)
- Calls `AuthService.login()` on submit
- On success: navigates to `/dashboard`
- On failure: displays error message from BFF
- Auth guard redirects unauthenticated users to `/login`
- Session checked on app init via `APP_INITIALIZER`

| ID | Story | Tasks | Complexity | Status |
|---|---|---|---|---|
| US-030 | As a user, I can log in with my email and password | T-092 Implement login-page reactive form<br>T-093 Wire AuthService.login() to form submit<br>T-094 Add error display block<br>T-095 Add loading spinner (PrimeNG ProgressSpinner) | M | [ ] |
| US-031 | As a user, I am redirected to login if my session expires | T-096 Update authGuard to call checkSession() on activation<br>T-097 Add APP_INITIALIZER to call checkSession on boot | S | [ ] |

**Dependencies:** EP-011 (BFF auth endpoints must exist)
**Risks:** APP_INITIALIZER with Observable must complete before app renders — use `firstValueFrom` pattern

---

### EP-016 — appsettings.Development.json

**Goal:** Local development config for all services so developers can run without Docker.

**Acceptance Criteria:**
- `appsettings.Development.json` in STRIDE.Host and STRIDE.BFF
- Local SQL Server connection string
- Local Redis connection string (`localhost:6379`)
- Local Seq URL (`http://localhost:5341`)
- Serilog minimum level: Debug for Development
- JWT secret for local development (non-production value)

| ID | Story | Tasks | Complexity | Status |
|---|---|---|---|---|
| US-032 | As a developer, I can run the backend locally without Docker using appsettings.Development.json | T-098 Write STRIDE.Host/appsettings.Development.json<br>T-099 Write STRIDE.BFF/appsettings.Development.json | XS | [ ] |

**Dependencies:** EP-011
**Risks:** Ensure `appsettings.Development.json` is gitignored if it contains real secrets (use .env.local pattern)

---

### Sprint 2 Dependency Order

```
EP-008 (Identity Domain)
  └─► EP-009 (Tenant Domain)
        └─► EP-010 (EF Core + Migrations)
              ├─► EP-011 (JWT + BFF Auth)
              │     └─► EP-015 (Angular Login UI)
              ├─► EP-012 (Commands & Queries)
              │     └─► EP-013 (Identity API)
              │           └─► EP-014 (RBAC)
              └─► EP-016 (appsettings.Development.json)
```

---

### Sprint 2 Architectural Risks

| Risk | Severity | Mitigation |
|---|---|---|
| TenantMiddleware fires before tenant is resolved during login | HIGH | Login endpoint explicitly bypasses TenantMiddleware; BFF /login resolves tenant before session creation |
| JWT secret in source control | HIGH | Use .env + User Secrets for local dev; never commit secrets |
| Cross-tenant session bleed in Redis | HIGH | Enforce `tenant:{tenantId}:session:{sessionId}` key pattern everywhere |
| Claims mismatch between JWT issuer and ASP.NET Core claim names | MEDIUM | Map `tid` → `TenantId`, `sub` → `UserId` in middleware; document claim names |
| EF Core migration conflicts in team environment | MEDIUM | One migration author per sprint; others pull before adding |
| BFF /me endpoint called on every route activation | LOW | Cache session in AuthService signal; only call BFF on cold start or 401 |

---

### Sprint 2 Cross-Cutting Concerns

| Concern | Strategy |
|---|---|
| Tenant resolution at login | `TenantResolver` in Identity.Infrastructure called from `LoginCommand` handler |
| Session storage | Redis with tenant-prefixed keys; TTL matches JWT expiry |
| Logging | TenantId + CorrelationId in every log; set after tenant resolution |
| Soft-delete | All new entities inherit `AuditableEntity.IsDeleted` — TenantAwareRepository filters automatically |
| Validation | FluentValidation validators for all commands; ValidationBehaviour pipeline behaviour |
| Error responses | `Result<T>` mapped to `ProblemDetails` at controller boundary |

---

## Cross-Sprint Backlog Items

These stories are not phase-specific — they run alongside regular sprints at defined checkpoints.

---

### US-033 — Architecture Diagrams (GitHub #36)

**Type:** Documentation / Architecture
**When:** Produced at the end of each respective phase/sprint. Diagram set grows incrementally.
**Tooling:** TBD — evaluating free AI-assisted diagramming tools (Mermaid Live, Eraser.io, Lucidchart AI). Goal: generate from a detailed text prompt, export as PNG + source.

| Diagram | Trigger | Status |
|---|---|---|
| System Context Diagram (C4 L1) | End of Phase 1 (retroactive) | [ ] |
| Container Diagram (C4 L2) | End of Phase 2 | [ ] |
| Module Interaction Diagram | End of Phase 2 | [ ] |
| Auth Flow Diagram | End of Phase 2 | [ ] |
| Tenant Resolution Flow | End of Phase 2 | [ ] |
| Deployment Diagram (Azure) | End of Phase 7 | [ ] |

**Output:** All diagrams committed to `/docs/diagrams/` as PNG + source file.

---

### US-034 — UI/UX Design (GitHub #37)

**Type:** Design prerequisite
**When:** Must be completed **before US-030 (login page)** and **before any Phase 3+ Angular UI stories**.
**Status:** [ ] Backlog

**Scope:**
- Color palette, typography, spacing system
- Component library selection confirmed (PrimeNG — already decided; this defines which components map to which screens)
- Wireframes for: Login, Dashboard, Workflow List, Workflow Detail, Scheduling, Reporting, Admin
- Responsive breakpoints defined (desktop-first — internal ops tool)
- Loading/empty/error states per screen

**Tooling:** Figma free tier (recommended) or AI-assisted wireframe tool (e.g. Uizard, Visily free tier).

**Acceptance Criteria:**
- [ ] Figma file (or equivalent) shared/committed with all screen wireframes
- [ ] Color tokens defined (primary, secondary, surface, text, error, success)
- [ ] All PrimeNG components mapped to each screen
- [ ] Mobile-responsive breakpoints noted (tablet + desktop minimum)
- [ ] Design reviewed and approved before any frontend Angular story begins

---

## Sprint 3 — Core Workflow Engine (PLANNED)

**Phase:** Phase 3
**Status:** PENDING — not started

High-level epics (to be expanded before sprint start):
- EP-017: Workflow Domain Model (Workflow, Task, State Machine)
- EP-018: Workflow Application Layer (Commands/Queries/Events)
- EP-019: Workflow Infrastructure (EF Core + `workflows` schema)
- EP-020: Scheduling Module (recurring tasks, scheduled triggers)
- EP-021: Workflow API Layer (endpoints, MediatR dispatch)
- EP-022: Angular Workflow UI (list, detail, create, status)

---

## Sprint 4 — Dashboard & Reporting (PLANNED)

**Phase:** Phase 4 — not started
**Key note:** Dapper read models for reporting queries (ADR-005)

---

## Sprint 5 — Notifications & Observability (PLANNED)

**Phase:** Phase 5 — not started

---

## Sprint 6 — SaaS Readiness (PLANNED)

**Phase:** Phase 6 — not started

---

## Sprint 7 — Portfolio & Deployment Polish (PLANNED)

**Phase:** Phase 7 — not started

---

## Architectural Milestones Tracker

| Milestone | Target Sprint | Status |
|---|---|---|
| Monorepo builds on .NET 10 | S1 | [x] Done |
| Frontend builds with Angular 21 + Tailwind 4 | S1 | [x] Done |
| Phase 1 committed to GitHub | S1 | [x] Done |
| Tenant-scoped login with Redis session | S2 | [ ] |
| RBAC claims authorization wired | S2 | [ ] |
| Angular auth guard + BFF session check | S2 | [ ] |
| Workflow domain model + state machine | S3 | [ ] |
| EF Core + Dapper split strategy proven | S4 | [ ] |
| All modules observable (Serilog + Seq + OTEL) | S5 | [ ] |
| Tenant onboarding flow complete | S6 | [ ] |
| Deployed to Azure with CI/CD | S7 | [ ] |

---

## Required Shared Abstractions Summary

| Abstraction | Layer | Phase Introduced | Notes |
|---|---|---|---|
| `IDomainEvent` | BuildingBlocks.Domain | 1 | [x] Implemented |
| `IIntegrationEvent : INotification` | BuildingBlocks.Application | 1 | [x] Implemented |
| `IEventBus` | BuildingBlocks.Application | 1 | [x] Implemented (MediatR-backed) |
| `IBackgroundTaskQueue` | BuildingBlocks.Application | 1 | [x] Interface only |
| `ITenantContext` | BuildingBlocks.Application | 1 | [x] Implemented |
| `ICurrentUser` | BuildingBlocks.Application | 1 | [x] Implemented |
| `Result<T>` | BuildingBlocks.Application | 1 | [x] Implemented |
| `TenantAwareRepository<T, TContext>` | BuildingBlocks.Infrastructure | 1 | [x] Implemented |
| `IJwtTokenService` | Identity.Application | 2 | [ ] Pending |
| `ISessionStore` | BuildingBlocks.Infrastructure or Identity.Infrastructure | 2 | [ ] Pending |
| `ITenantResolver` | BuildingBlocks.Infrastructure | 1 (interface), 2 (impl) | [ ] Implementation pending |
| `IPasswordHasher` | Identity.Application | 2 | [ ] Pending |
| `IUserRepository` | Identity.Application | 2 | [ ] Pending |
| `ITenantRepository` | Identity.Application | 2 | [ ] Pending |
