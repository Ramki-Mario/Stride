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
**Status:** ✅ COMPLETE — All stories done. EP-015 closed. Architecture diagrams (US-036–039) unblocked.

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
| US-025 | As a developer, JWT configuration is environment-driven | T-079 Add Jwt:Secret, Jwt:Issuer, Jwt:ExpiryMinutes to appsettings + .env.example<br>T-080 Wire AddJwtBearer on STRIDE.Host (fail-fast if secret missing) | XS | [x] |

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
| US-026 | As a developer, all identity operations flow through MediatR handlers | T-080 LoginCommand + handler<br>T-081 RegisterCommand + handler<br>T-082 AssignRoleCommand + handler<br>T-083 GetUserQuery + handler<br>T-084 PBKDF2 PasswordHasher<br>T-085 ITenantContextSetter + TenantMiddleware fix | L | [x] |
| US-027 | As an operator, invalid login attempts return structured errors, not exceptions | T-085 LoginCommandValidator<br>T-086 RegisterCommandValidator<br>T-087 AssignRoleCommandValidator<br>T-088 GetUserQueryValidator<br>T-089 Wire LoggingBehaviour+ValidationBehaviour into Identity MediatR pipeline | S | [x] |

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
| US-028 | As a developer, Identity API endpoints are thin, MediatR-dispatching controllers | T-086 AuthController (login + register)<br>T-087 UsersController (GetById + AssignRole)<br>T-088 Request DTOs (LoginRequest, RegisterRequest, AssignRoleRequest)<br>T-089 CurrentUser (ICurrentUser impl reading JWT claims)<br>T-090 GlobalExceptionHandler (ValidationException->400, unhandled->500) | M | [x] |

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
| US-029 | As a developer, I can protect any endpoint with a permission policy in one attribute | T-089 Policies.cs constants (6 named policies)<br>T-090 AuthorizationPoliciesExtensions (module self-registers)<br>T-091 IdentityModuleExtensions calls AddIdentityAuthorizationPolicies<br>T-092 UsersController.AssignRole -> RequireAdmin | S | [x] |

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
| US-030 | As a user, I can log in with my email and password | T-092 Implement login-page reactive form<br>T-093 Wire AuthService.login() to form submit<br>T-094 Add error display block<br>T-095 Add loading spinner (CSS-only) | M | [x] Done — PR #70 (2026-05-15) |
| US-031 | As a user, I am redirected to login if my session expires | T-096 Update authGuard to call checkSession() on activation<br>T-097 Add APP_INITIALIZER to call checkSession on boot | S | [x] Done — PR #71 (2026-05-15) |

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
| US-032 | As a developer, I can run the backend locally without Docker using appsettings.Development.json | T-098 Write STRIDE.Host/appsettings.Development.json<br>T-099 Write STRIDE.BFF/appsettings.Development.json | XS | [x] PR #54 |

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

### EP-017 — Architecture Diagrams (GitHub #46, Milestone: Docs and Diagrams)

**Type:** Documentation / Architecture
**Milestone:** "Docs and Diagrams" (GitHub Milestone #8) — home for all future doc/diagram stories.
**Tooling:** Mermaid Live / Eraser.io / Lucidchart AI. Output: PNG + source in `/docs/diagrams/`.
**Old US-033 (#36) closed** — superseded by individual per-diagram stories below.

| Story | GitHub # | Diagram | Trigger | Status |
|---|---|---|---|---|
| US-035 | #47 | System Context Diagram (C4 L1) | End of Phase 1 (retroactive) | [x] PR #53 |
| US-036 | #48 | Container Diagram (C4 L2) | End of Phase 2 | [ ] |
| US-037 | #49 | Module Interaction Diagram | End of Phase 2 | [ ] |
| US-038 | #50 | Auth Flow Diagram | End of Phase 2 | [ ] |
| US-039 | #51 | Tenant Resolution Flow Diagram | End of Phase 2 | [ ] |
| US-040 | #52 | Deployment Diagram (Azure) | End of Phase 7 | [ ] |

**Output:** Each diagram committed to `/docs/diagrams/{name}.png` + source file.

---

### US-034 — UI/UX Design (GitHub #37)

**Type:** Design prerequisite
**When:** Must be completed **before US-030 (login page)** and **before any Phase 3+ Angular UI stories**.
**Status:** [-] IN PROGRESS — 5 of 7 sub-issues Done

**Design System established (PR #64, 2026-05-15):**
- Official palette: Indigo `#6366F1` / Purple `#8B5CF6` (Premium SaaS, Linear-inspired)
- `frontend/src/styles.scss` — STRIDE CSS tokens, Tailwind `@theme inline` bridge, PrimeNG `--p-*` overrides
- `frontend/src/app/core/theme/theme.service.ts` — Signal-based ThemeService
- Light + dark theme via `data-theme` attribute; flash-prevention inline script in `index.html`
- `providePrimeNG({ ripple: true, inputVariant: 'outlined' })` in `app.config.ts`

**HTML Prototype Sub-Issues:**

| Story | File | PR | Status |
|---|---|---|---|
| US-034.1 Shell & Layout (#55) | `01-shell-layout.html` | #62 | [x] Done |
| US-034.2 Login Page (#56) | `02-login.html` | #63 | [x] Done |
| US-034.3 Dashboard (#57) | `03-dashboard.html` | #65 | [x] Done |
| US-034.4 Workflow List (#58) | `04-workflow-list.html` | #66 | [x] Done |
| US-034.5 Workflow Detail (#59) | `05-workflow-detail.html` | #67 | [x] Done |
| US-034.6 User Management (#60) | `06-user-management.html` | #68 | [x] Done |
| US-034.7 Reporting (#61) | `07-reporting.html` | #69 | [x] Done |

**Acceptance Criteria:**
- [x] Color tokens defined — `--stride-*` CSS custom properties, light + dark
- [x] Responsive breakpoints: desktop-first, collapses at 1024px (tablet) and 768px (mobile)
- [x] Loading/error/empty states per screen
- [x] Shell, Login, Dashboard, Workflow List, Workflow Detail prototypes committed
- [ ] User Management prototype (#60)
- [ ] Reporting prototype (#61)
- [x] US-034 parent issue #37 closed ✅ — all 7 sub-issues done

---

## Sprint 3 — Core Workflow Engine (COMPLETE)

**Phase:** Phase 3
**Status:** ✅ COMPLETE — EP-018 ✅ EP-019 ✅ EP-020 ✅ EP-021 ✅ EP-022 ✅ (2026-05-16)

| Epic | GitHub # | Stories | Status |
|---|---|---|---|
| EP-018 Workflow Domain Model | #72 | US-041 #77 ✅, US-042 #78 ✅, US-043 #79 ✅ | ✅ Done — PR #104 |
| EP-019 Workflow Application Layer | #73 | US-044 #80 ✅, US-045 #81 ✅, US-046 #82 ✅, US-047 #83 ✅, US-048 #84 ✅ | ✅ Done — PR #105 |
| EP-020 Workflow Infrastructure | #74 | US-049 #85 ✅, US-050 #86 ✅, US-051 #87 ✅ | ✅ Done — PR #106 |
| EP-021 Workflow API Layer | #75 | US-052 #88 ✅, US-053 #89 ✅ | ✅ Done — PR #107 |
| EP-022 Angular Workflow UI | #76 | US-054 #90 ✅, US-055 #91 ✅, US-056 #92 ✅, US-057 #93 ✅ | ✅ Done — PRs #108 #110 #111 #112 |

### Phase 3 Story Summary
| Story | Description | GitHub # | Size |
|---|---|---|---|
| US-041 | WorkflowDefinition + WorkflowInstance aggregates | #77 | M |
| US-042 | Workflow state machine + domain events | #78 | M |
| US-043 | Step + StepInstance entities | #79 | S |
| US-044 | CreateWorkflow + UpdateWorkflow + DeleteWorkflow commands | #80 | M |
| US-045 | Workflow lifecycle commands (Start, Pause, Resume, Cancel) | #81 | M |
| US-046 | Step commands (AssignStep, CompleteStep, FailStep) | #82 | M |
| US-047 | Workflow queries (GetWorkflow, ListWorkflows, GetWorkflowHistory) | #83 | S |
| US-048 | FluentValidation validators for workflow commands | #84 | S |
| US-049 | EF Core configs + workflows schema migration | #85 | M |
| US-050 | IWorkflowRepository + IStepRepository implementations | #86 | M |
| US-051 | Dapper read models for workflow list and dashboard queries | #87 | M |
| US-052 | WorkflowsController (CRUD + lifecycle endpoints) | #88 | M |
| US-053 | StepsController (assign, complete, fail, list) | #89 | S |
| US-054 | Angular Workflow list page | #90 | M |
| US-055 | Angular Workflow detail page | #91 | M |
| US-056 | Create + Edit workflow form | #92 | M |
| US-057 | Step assign + complete modals | #93 | S |

---

## Sprint 3.1 — ThemeBuilder / Bring Your Own Theme (IN PROGRESS)

**Phase:** Phase 3.1
**Milestone:** Bring Your Own Theme (GitHub Milestone #9)
**Status:** ✅ COMPLETE — EP-027 ✅ EP-028 ✅ EP-029 ✅ (2026-05-16)

> Phase 3.1 is a design-system sub-sprint that runs after the core Workflow Engine (Phase 3) and before Dashboard & Reporting (Phase 4). It consolidates all palette and theme work under EP-029 ThemeBuilder.

| Epic | GitHub # | Stories | Status |
|---|---|---|---|
| EP-029 ThemeBuilder (parent) | #127 | — | ✅ Done — closed |
| EP-027 BYOT Foundation | #117 | US-064 #115 ✅, US-065 #118 ✅, US-066 #119 ✅ | ✅ Done — closed (Phase 6: US-067–070 deferred) |
| EP-028 Premium Purple Design System | #120 | US-071–075 ✅, US-076 #128 ✅, US-077 #129 ✅ | ✅ Done — closed |

---

### EP-029 — ThemeBuilder (GitHub #127, Milestone: Bring Your Own Theme)

**Type:** Parent Epic
**Phase:** Phase 3.1 → Phase 6 (BYOT extension)
**Goal:** Consolidate all theme and palette work under a single ThemeBuilder epic. Establishes STRIDE's BYOT capability — from the foundational multi-palette switcher through the Premium Purple design system to future tenant-supplied CSS token customisation.

**Acceptance Criteria:**
- [ ] Multi-palette architecture ships: Purple (default) + Indigo (`data-palette="indigo"`)
- [ ] Palette composes with `data-theme` (light/dark) — 4 combinations work
- [ ] Selected palette persists across sessions via `localStorage`
- [ ] Flash-of-wrong-palette prevented by inline script in `index.html`
- [ ] Premium Purple is the root default; no attribute needed
- [ ] Light sidebar, frosted-glass topbar, analytics card gradients delivered
- [ ] Phase 6: tenant-supplied palette stored in TenantSettings, validated server-side, preview available

---

### EP-027 — BYOT Foundation (GitHub #117) — sub-epic of EP-029

**Goal:** Multi-palette switcher, ThemeService palette signal, and anti-flash script. Establishes the `data-palette` CSS architecture that all future palettes build on.

**Acceptance Criteria:**
- [x] At least two palettes (Indigo + Purple) switch without page reload
- [x] Selected palette persists across sessions via `localStorage`
- [x] Flash-of-wrong-palette prevented by inline script in `index.html`
- [ ] Palette stored per tenant in `TenantSettings` (Phase 6 integration)
- [ ] Tenant-supplied palette validated and sanitised before application

| ID | Story | GitHub # | Complexity | Status |
|---|---|---|---|---|
| US-064 | As a user, I can switch between Indigo and Purple palettes from the topbar | #115 | S | [x] Done — PR #116 |
| US-065 | As a developer, ThemeService manages palette state with signal + localStorage persistence | #118 | S | [x] Done — PR #116 |
| US-066 | As a developer, the anti-flash script applies both theme mode and palette before Angular boots | #119 | XS | [x] Done — PR #116 |
| US-067 | As a tenant admin, I can configure a default colour palette for my tenant in Settings | — | M | [ ] Phase 6 |
| US-068 | As a tenant admin, I can upload a custom CSS token set during tenant onboarding (BYOT) | — | L | [ ] Phase 6 |
| US-069 | As a developer, tenant-supplied palette tokens are validated and sanitised server-side | — | M | [ ] Phase 6 |
| US-070 | As a tenant admin, I can preview my custom palette before saving it live | — | M | [ ] Phase 6 |

**Dependencies:** EP-022 (Angular Shell — provides topbar host), Phase 6 (tenant onboarding flow)
**Risks:** Untrusted tenant CSS must be sanitised server-side to prevent XSS via custom properties.
**Phase 3.1 Status:** ✅ Closed 2026-05-16 — Phase 6 stories (US-067–070) deferred to Phase 6 epic.

---

### EP-028 — Premium Purple Design System (GitHub #120) — sub-epic of EP-029

**Goal:** Replace the Indigo default palette with the ChatGPT-analysed premium Purple design system. Production-ready token overhaul, light-sidebar layout, frosted-glass topbar, analytics card gradients, full PrimeNG alignment.

**Background:** Purple (`#B97AF9`) replaces Indigo (`#6366F1`) as the root default; Indigo preserved as `data-palette="indigo"` for tenants who prefer it.

**Acceptance Criteria:**
- [x] `:root` tokens updated to Premium Purple palette — primary `#B97AF9`, accent `#E15CFA`
- [x] Light sidebar (`#FFFFFF` bg, muted text, purple active items)
- [x] Dark sidebar preserved (`#111827`)
- [x] Topbar frosted glass (`rgba(255,255,255,0.85)` + `backdrop-filter: blur(12px)`)
- [x] `--stride-nav-brand-text` token prevents white-on-white brand name
- [x] Analytics chart token set (`--stride-chart-*`) for future chart components
- [x] `.stride-analytic-card--{variant}` gradient helper classes
- [x] `[data-palette="indigo"]` overrides preserve classic system
- [x] `ThemeService` defaults to `'purple'`; indigo applied via `data-palette="indigo"`
- [x] Anti-flash script updated to match new palette default logic
- [x] PrimeNG card shadow uses `--stride-shadow-card` (purple-tinted)
- [x] Sidebar right-border separating it from main content (light mode polish) → **US-076 #128 — PR #130**
- [x] Login page restyled for new palette → **US-077 #129 — PR #131**

| ID | Story | GitHub # | Complexity | Status |
|---|---|---|---|---|
| US-071 | As a designer, the default STRIDE palette is Premium Purple with soft lavender aesthetics | #121 | M | [x] Done — PR #116 |
| US-072 | As a developer, the light sidebar uses white surface with purple active states | #122 | S | [x] Done — PR #116 |
| US-073 | As a developer, the topbar uses frosted-glass backdrop-filter in both themes | #123 | S | [x] Done — PR #116 |
| US-074 | As a developer, analytics card gradient helpers are available as `.stride-analytic-card--*` | #124 | XS | [x] Done — PR #116 |
| US-075 | As a tenant admin, the Indigo palette is preserved as an alternative via the palette switcher | #125 | S | [x] Done — PR #116 |
| US-076 | As a user, the sidebar has a right-border in light mode separating it from main content | #128 | XS | [x] Done — PR #130 |
| US-077 | As a user, the login page uses Premium Purple design tokens consistently | #129 | S | [x] Done — PR #131 |

**Dependencies:** EP-022 (shell layout), EP-027 (BYOT palette switcher in topbar)
**Risks:** Light sidebar requires `--stride-nav-brand-text` token in all future sidebar implementations.

---

## Sprint 4 — Dashboard & Reporting (COMPLETE ✅)

**Phase:** Phase 4
**Status:** ✅ COMPLETE — All stories done (US-058–063), all epics closed (EP-023/024/025/026). PRs #132–#140 merged.
**Key patterns established:**
- Dapper read models + SqlLoader + embedded .sql pattern (ADR-005)
- PrimeNG `<p-select>` pattern for all future dropdowns
- JWT `MapInboundClaims = false` mandatory — claim names: `"sub"`, `"email"`, `"tid"`, `ClaimTypes.Role`
- `DateOnlyTypeHandler` required for any Dapper read model with `DateOnly` fields
- JSON field name alignment: always verify C# property → camelCase → Angular interface (root cause of "Invalid Date")
- `WorkflowInstanceDto` has no `totalSteps`/`completedSteps`; derive from `steps.length` + filter
- Feature folder convention: `features/dashboard/` not `features/reporting/` for dashboard pages
- `DateOnly` → JS: always `new Date(\`${iso}T00:00:00\`)` to avoid UTC-offset wrong-day issue

| Epic | GitHub # | Stories | Status |
|---|---|---|---|
| EP-023 Reporting Read Models | #94 | US-058 #98 | ✅ Done — closed |
| EP-024 Reporting Application Layer | #95 | US-059 #99, US-060 #100 | ✅ Done — closed |
| EP-025 Reporting API Layer | #96 | US-061 #101 | ✅ Done — closed |
| EP-026 Angular Dashboard + Reporting UI | #97 | US-062 #102, US-063 #103 | ✅ Done — closed |

### Phase 4 Story Summary
| Story | Description | GitHub # | Size | Status |
|---|---|---|---|---|
| US-058 | Dapper reporting DTOs + reporting schema | #98 | M | ✅ Done — PR #132 |
| US-059 | KPI + trend aggregation query handlers | #99 | M | ✅ Done — PR #133 |
| US-060 | GetReportList + GenerateReport + ExportReportCsv handlers | #100 | M | ✅ Done — PR #133 |
| US-061 | DashboardController + ReportsController | #101 | M | ✅ Done — PR #135 |
| US-062 | Angular Dashboard page | #102 | M | ✅ Done — PR #136 |
| US-063 | Angular Reporting page + bug fixes | #103 | M | ✅ Done — PRs #138–#140 |

---

## Sprint 5 — Notifications & Observability (PLANNED)

**Phase:** Phase 5
**Status:** 🔵 Planning — stories defined, GitHub issues pending.
**Goal:** Deliver the Notifications module end-to-end (domain → API → Angular) and harden observability (Serilog enrichers, SQL/Redis health checks, Angular health dashboard).

**Already in place (no rework needed):**
- `CorrelationIdMiddleware` + `X-Correlation-Id` header ✅
- `LoggingBehaviour` (MediatR pipeline, logs command/query name + duration) ✅
- Basic Serilog config (Console sink, Development level=Debug) ✅
- Basic health endpoints `/health`, `/health/live`, `/health/ready` ✅
- `Notifications` module scaffold (4-layer skeleton) ✅

**Phase 5 scope gaps:**
- Notification aggregate + repository + notifications schema migration
- CreateNotification / MarkAsRead / Dismiss commands + GetNotifications / UnreadCount queries
- Domain event → notification bridge (workflow events → auto-created notifications)
- NotificationsController + BFF proxy
- Angular topbar bell with unread badge + dropdown + `/notifications` page
- Serilog context enrichers (TenantId, UserId on every log line)
- SQL Server + Redis health checks for `/health/ready`
- Angular Health dashboard page at `/administration/health`

| Epic | GitHub # | Stories | Status |
|---|---|---|---|
| EP-030 Notifications Domain | #141 | US-078 #146, US-079 #147 | [ ] Pending |
| EP-031 Notifications Application Layer | #142 | US-080 #148, US-081 #149, US-082 #150 | [ ] Pending |
| EP-032 Notifications API + BFF | #143 | US-083 #151, US-084 #152 | [ ] Pending |
| EP-033 Angular Notifications UI | #144 | US-085 #153, US-086 #154 | [ ] Pending |
| EP-034 Observability | #145 | US-087 #155, US-088 #156, US-089 #157 | [ ] Pending |

### Phase 5 Story Summary
| Story | Description | GitHub # | Size | Status |
|---|---|---|---|---|
| US-078 | Notification aggregate — entity, NotificationType enum, domain events | #146 | S | [ ] |
| US-079 | NotificationRepository + EF Core config + notifications schema migration | #147 | S | [ ] |
| US-080 | CreateNotification / MarkAsRead / DismissNotification commands + handlers | #148 | M | [ ] |
| US-081 | GetNotificationsQuery (paginated, unread filter) + GetUnreadCountQuery | #149 | S | [ ] |
| US-082 | Domain event → notification bridge (workflow events → notifications) | #150 | M | [ ] |
| US-083 | NotificationsController: GET /notifications, PATCH /{id}/read, DELETE /{id}, GET /unread-count | #151 | S | [ ] |
| US-084 | BFF NotificationsApiClient + BFF NotificationsController proxy (/bff/notifications/*) | #152 | XS | [ ] |
| US-085 | Angular topbar bell: unread count badge, dropdown, mark-as-read | #153 | M | [ ] |
| US-086 | Angular /notifications page: full list, read/unread filter, dismiss, empty state | #154 | M | [ ] |
| US-087 | Serilog enrichers: TenantId + UserId on every structured log line | #155 | S | [ ] |
| US-088 | SQL Server + Redis health checks tagged "ready"; detailed health response body | #156 | S | [ ] |
| US-089 | Angular /administration/health page: module status cards, auto-refresh | #157 | M | [ ] |

---

### EP-030 — Notifications Domain Model

**Goal:** Define the `Notification` aggregate that captures who received what event and their read state.

**Acceptance Criteria:**
- `Notification` extends `AuditableEntity` — has `RecipientId` (UserId), `TenantId`, `Type`, `Title`, `Body`, `IsRead`, `IsDeleted`, `CreatedAt`
- `NotificationType` enum: `WorkflowStarted`, `WorkflowCompleted`, `WorkflowFailed`, `StepAssigned`, `StepCompleted`, `SystemAlert`
- `Notification.Create(tenantId, recipientId, type, title, body)` factory
- `Notification.MarkAsRead()` — sets `IsRead = true`, `UpdatedAt`
- Domain event: `NotificationCreatedEvent`
- `INotificationRepository` in Application layer

| ID | Story | Size | Status |
|---|---|---|---|
| US-078 | Notification aggregate entity + NotificationType enum + domain events + INotificationRepository | S | [ ] |
| US-079 | NotificationConfiguration (EF Core) + InitialCreate migration (notifications schema) + NotificationRepository | S | [ ] |

**Dependencies:** BuildingBlocks.Domain, EF Core patterns from Identity/Workflows

---

### EP-031 — Notifications Application Layer

**Goal:** MediatR commands/queries for notification lifecycle + domain event handlers that auto-create notifications from workflow events.

**Acceptance Criteria:**
- `CreateNotificationCommand(tenantId, recipientId, type, title, body)` + handler
- `MarkAsReadCommand(notificationId, userId)` + handler — validates recipient ownership
- `DismissNotificationCommand(notificationId, userId)` + handler — soft-deletes
- `GetNotificationsQuery(userId, page, pageSize, unreadOnly)` → paged `NotificationDto` list
- `GetUnreadCountQuery(userId)` → `int`
- `WorkflowEventNotificationHandler` — handles `WorkflowStartedEvent`, `WorkflowCompletedEvent`, `WorkflowFailedEvent`, `StepAssignedEvent` domain events → dispatches `CreateNotificationCommand`
- All commands/queries have FluentValidation validators

| ID | Story | Size | Status |
|---|---|---|---|
| US-080 | CreateNotification + MarkAsRead + DismissNotification commands + handlers + validators | M | [ ] |
| US-081 | GetNotificationsQuery (paginated, unread filter) + GetUnreadCountQuery + NotificationDto | S | [ ] |
| US-082 | WorkflowEventNotificationHandler: bridge workflow domain events → CreateNotificationCommand | M | [ ] |

**Dependencies:** EP-030, Workflow domain events

---

### EP-032 — Notifications API + BFF Proxy

**Goal:** Thin NotificationsController + BFF forwarding layer.

**Acceptance Criteria:**
- `GET /api/notifications?page=1&pageSize=20&unreadOnly=false` → paged list
- `GET /api/notifications/unread-count` → `{ count: N }`
- `PATCH /api/notifications/{id}/read` → 204
- `DELETE /api/notifications/{id}` → 204 (dismiss/soft-delete)
- All endpoints `[Authorize]`, tenant-scoped via `ITenantContext`
- BFF `NotificationsApiClient` + BFF `NotificationsController` at `/bff/notifications/*`

| ID | Story | Size | Status |
|---|---|---|---|
| US-083 | NotificationsController (4 endpoints) + request DTOs | S | [ ] |
| US-084 | NotificationsApiClient typed HttpClient + BFF NotificationsController proxy | XS | [ ] |

**Dependencies:** EP-031

---

### EP-033 — Angular Notifications UI

**Goal:** Topbar bell with badge + dropdown, plus full notifications list page.

**Acceptance Criteria:**
- Bell icon in topbar shows unread count badge (0 = hidden, 1–9 = digit, 10+ = "9+")
- Clicking bell opens dropdown: 5 most recent notifications, "Mark all read" button, "View all" link
- Polling every 30 seconds for unread count (or on page focus)
- `/notifications` page: full list, unread/all toggle filter, mark single as read, dismiss, skeleton loader, empty state
- `NotificationsService` at `/bff/notifications` — uses `HttpClient`
- `NotificationDto` Angular interface matches backend camelCase names

| ID | Story | Size | Status |
|---|---|---|---|
| US-085 | TopbarComponent: bell icon, unread badge, dropdown panel, mark-all-read | M | [ ] |
| US-086 | `/notifications` page: list, read/unread filter, mark-as-read, dismiss, skeleton, empty state | M | [ ] |

**Dependencies:** EP-032

---

### EP-034 — Observability

**Goal:** Enrich every structured log with TenantId + UserId; add SQL Server + Redis health checks; Angular health dashboard.

**Acceptance Criteria:**
- `TenantEnricher` + `UserEnricher` Serilog enrichers — pull from `ITenantContext` + `ICurrentUser`, add `TenantId` + `UserId` to every log event scope
- Registered in `UseSerilog` lambda in Host `Program.cs`
- SQL Server health check (database ping, tagged `"ready"`) via `AddSqlServer`
- Redis health check (PING command, tagged `"ready"`) via `AddRedis`
- `/health/ready` returns 200 + full JSON body listing each check name + status + duration
- Angular `/administration/health` page: cards per module (Host DB, Redis, BFF), colour-coded status, last-checked timestamp, manual refresh button, auto-refresh every 60 seconds

| ID | Story | Size | Status |
|---|---|---|---|
| US-087 | Serilog TenantId + UserId enrichers registered in Host Program.cs | S | [ ] |
| US-088 | SQL Server + Redis health checks tagged "ready"; verbose health response body | S | [ ] |
| US-089 | Angular /administration/health page: module status cards, auto/manual refresh | M | [ ] |

**Dependencies:** Serilog already wired; `AspNetCore.HealthChecks.SqlServer` + `AspNetCore.HealthChecks.Redis` NuGet packages needed

---

## Sprint 6 — SaaS Readiness (PLANNED)

**Sprint Goal:** Bring the two remaining stub modules to life (Administration + Invoicing), deliver BYOT per-tenant customisation, build the tenant self-service onboarding wizard, and harden the platform with rate limiting and an admin audit trail. Meets the "Tenant onboarding flow complete" architectural milestone.

**Phase:** Phase 6
**Status:** ✅ COMPLETE — All 5 epics done (2026-05-30). Commit `cdba97f`.

**Modules activated this phase:** Administration (from stub), Invoicing (from stub)
**Deferred stories resolved:** US-067–070 (BYOT from EP-027) land as US-103–105 inside EP-037.
**Issue #109 note:** BFF workflow proxy (`WorkflowApiClient` + BFF `WorkflowsController`) was wired in Phase 4 (PR #138) — already complete, no Phase 6 work needed.

| Epic | GitHub # | Stories | Status |
|---|---|---|---|
| EP-035 Tenant User Administration | #163 | US-090–094 (#168–172) | [x] Done |
| EP-036 Invoicing Core | #164 | US-095–099 (#173–177) | [x] Done |
| EP-037 Tenant Settings & BYOT | #165 | US-100–105 (#178–183) | [x] Done |
| EP-038 Tenant Onboarding Flow | #166 | US-106–108 (#184–186) | [x] Done |
| EP-039 SaaS Hardening | #167 | US-109–111 (#187–189) | [ ] Next |

### Phase 6 Story Summary
| Story | Description | GitHub # | Size | Status |
|---|---|---|---|---|
| US-090 | GetUsersQuery: paginated, searchable, role + status filter | #168 | M | [x] |
| US-091 | InviteUserCommand: create pending user, fire in-app notification | #169 | M | [x] |
| US-092 | UpdateUserRoleCommand + DeactivateUser + ReactivateUser commands | #170 | S | [x] |
| US-093 | AdministrationController (5 endpoints) + BFF AdminApiClient + BFF proxy | #171 | S | [x] |
| US-094 | Angular /administration users page: table, search, filter, invite modal, row actions | #172 | L | [x] |
| US-095 | Invoice aggregate + InvoiceLineItem value object + InvoiceStatus enum + domain events | #173 | M | [ ] |
| US-096 | InvoiceConfiguration + EF Core migration + InvoiceRepository | #174 | S | [ ] |
| US-097 | GenerateInvoice / SendInvoice / MarkPaid / VoidInvoice commands + list/get queries | #175 | L | [ ] |
| US-098 | InvoicesController (6 endpoints) + InvoicingApiClient + BFF InvoicingController | #176 | M | [ ] |
| US-099 | Angular /invoicing page: list, summary cards, create modal, pay/void actions | #177 | L | [ ] |
| US-100 | TenantSettings entity + ITenantSettingsRepository + administration schema migration | #178 | S | [ ] |
| US-101 | GetTenantSettingsQuery + UpdateTenantSettingsCommand + handler + validator | #179 | S | [ ] |
| US-102 | TenantSettingsController (GET/PUT) + BFF proxy; /auth/me extended with defaultPalette | #180 | S | [ ] |
| US-103 | Tenant default palette applied on login from settings; ThemeService reads /auth/me | #181 | M | [ ] |
| US-104 | BYOT CSS token upload + server-side whitelist sanitiser + stored as JSON | #182 | L | [ ] |
| US-105 | Angular palette preview mode: apply tokens without saving; Preview/Save/Reset buttons | #183 | M | [ ] |
| US-106 | RegisterTenantCommand: creates Tenant + first Admin user atomically; Plan enum | #184 | L | [ ] |
| US-107 | TenantsController: POST /tenants/register (unauthenticated) + BFF proxy | #185 | S | [ ] |
| US-108 | Angular onboarding wizard: 4-step (org → admin → appearance → invite), auto-login | #186 | XL | [ ] |
| US-109 | Per-tenant rate limiting: sliding window 100 req/60s, 429 + Retry-After | #187 | M | [x] ✅ |
| US-110 | AuditLog entity + IAuditLogger + administration migration; injected into key handlers | #188 | M | [x] ✅ |
| US-111 | GetAuditLogQuery + AuditLogController + BFF proxy + Angular /administration/audit-log page | #189 | M | [x] ✅ |

---

### EP-035 — Tenant User Administration

**Goal:** Implement the Administration module fully: list tenant users, invite new members, manage roles, and deactivate/reactivate accounts. Replaces the current placeholder `/administration` page.

**Acceptance Criteria:**
- `GET /api/administration/users?page=1&pageSize=20&search=&role=&status=` → paged `AdminUserDto` list (via Dapper read model)
- `POST /api/administration/users/invite` → creates `User` with `IsActive = false`, `IsPending = true`; dispatches `CreateNotificationCommand` to notify the invitee
- `PUT /api/administration/users/{id}/role` → changes role (Admin / Member / Viewer); only Admin callers
- `PUT /api/administration/users/{id}/deactivate` + `PUT /api/administration/users/{id}/reactivate`
- All endpoints require `[Authorize(Policy = "CanManageUsers")]`
- BFF routes: `/bff/administration/users/*` forwarded to Host
- Angular `/administration` page: users table (name, email, role badge, status badge, last-active), search bar, role filter, status filter (`Active / Pending / Inactive`), Invite modal (email + role dropdown), row action menu (Change Role / Deactivate / Reactivate), skeleton loader, empty state, pagination

| ID | Story | Tasks | Complexity | Status |
|---|---|---|---|---|
| US-090 | As an admin, I can see a paginated, searchable list of my tenant's users | GetUsersQuery + handler + AdminUserDto + Dapper read (joins users, user_roles, tenants) | M | [x] |
| US-091 | As an admin, I can invite a new user to my tenant by email | InviteUserCommand + handler + validator; creates User (IsActive=false, IsPending=true); dispatches CreateNotificationCommand; IsPending flag on User entity | M | [x] |
| US-092 | As an admin, I can change a user's role, deactivate, or reactivate them | UpdateUserRoleCommand + DeactivateUserCommand + ReactivateUserCommand + handlers + validators | S | [x] |
| US-093 | As a developer, administration operations are exposed via API and BFF proxy | AdministrationController (5 endpoints) + AdminApiClient typed HttpClient + BFF AdminController proxy | S | [x] |
| US-094 | As an admin, I see a polished users management page with invite and edit modals | Angular /administration page: p-table + search + filter + invite modal (ReactiveForm) + row action menu + skeleton + empty state + pagination | L | [x] |

**Dependencies:** Identity module (User entity, IUserRepository, roles), Notifications module (CreateNotificationCommand)
**Risks:**
- Invite must enforce `TenantId` — never allow cross-tenant user creation
- `IsPending` flag needs adding to `User` entity and EF config (non-breaking migration)

---

### EP-036 — Invoicing Core

**Goal:** Implement the Invoicing module end-to-end: Invoice aggregate, generation/payment lifecycle, full API, BFF proxy, and an Angular invoicing page with summary KPIs.

**Acceptance Criteria:**
- `Invoice` aggregate: `InvoiceNumber` (tenant-scoped sequential, e.g. `INV-0001`), `TenantId`, `RecipientName`, `LineItems[]` (description, quantity, unitPrice), `TotalAmount` (computed), `Currency`, `Status` (`Draft / Sent / Paid / Overdue / Void`), `DueDate`, `IssuedAt`
- Domain events: `InvoiceGeneratedEvent`, `InvoiceSentEvent`, `InvoicePaidEvent`, `InvoiceVoidedEvent`
- Commands: `GenerateInvoice`, `SendInvoice`, `MarkInvoicePaid`, `VoidInvoice`
- Queries: `GetInvoiceById`, `ListInvoices (page, pageSize, status filter)`, `GetInvoiceSummary` (total count, total amount, paid, outstanding, overdue)
- `invoicing.*` SQL schema; EF Core migration
- REST: `GET /api/invoicing/invoices`, `POST /api/invoicing/invoices`, `GET /api/invoicing/invoices/{id}`, `POST /api/invoicing/invoices/{id}/send`, `POST /api/invoicing/invoices/{id}/pay`, `DELETE /api/invoicing/invoices/{id}` (void)
- Angular `/invoicing` page: summary KPI cards (Total / Paid / Outstanding / Overdue), invoice table (number, recipient, amount, status badge, due date), Create Invoice modal (line items builder), action buttons (Send / Mark Paid / Void), skeleton, empty state

| ID | Story | Tasks | Complexity | Status |
|---|---|---|---|---|
| US-095 | As a tenant admin, I have an Invoice aggregate with full lifecycle | Invoice entity + InvoiceLineItem value object + InvoiceStatus enum + InvoiceNumber generator + domain events + IInvoiceRepository | M | [ ] |
| US-096 | As a developer, invoices are persisted in the invoicing schema | InvoiceConfiguration + InvoiceLineItemConfiguration (owned entity) + invoicing schema migration + InvoiceRepository | S | [ ] |
| US-097 | As a tenant admin, I can generate, send, pay, and void invoices via commands | GenerateInvoice + SendInvoice + MarkInvoicePaid + VoidInvoice commands + handlers + validators; ListInvoices + GetInvoiceById + GetInvoiceSummary queries + handlers + Dapper reads | L | [ ] |
| US-098 | As a developer, invoicing operations are exposed via REST API and BFF proxy | InvoicesController (6 endpoints) + request DTOs + InvoicingApiClient typed HttpClient + BFF InvoicingController proxy | M | [ ] |
| US-099 | As a tenant admin, I can manage invoices from the Angular Invoicing page | Angular /invoicing page: summary KPI cards + p-table + Create modal (ReactiveForm, dynamic line items) + Send/Pay/Void row actions + skeleton + empty state | L | [ ] |

**Dependencies:** BuildingBlocks.Domain, EF Core patterns from Identity/Workflows
**Risks:**
- `InvoiceNumber` must be tenant-scoped sequential — use a Dapper `MAX(invoice_number) + 1` with optimistic retry or a dedicated `InvoiceSequence` table; avoid global IDENTITY
- `InvoiceLineItem` as an EF Owned Entity collection (`OwnsMany`) — same pattern as step definitions

---

### EP-037 — Tenant Settings & BYOT Customisation

**Goal:** Persist tenant configuration server-side (`TenantSettings` entity). Deliver the four deferred BYOT stories (US-067–070): per-tenant default palette applied on login, custom CSS token upload, server-side sanitisation, and live preview mode.

**Acceptance Criteria:**
- `TenantSettings` entity in Administration domain: `TenantId` (1:1 FK to Tenant), `DisplayName`, `DefaultPalette` (`purple` | `indigo`), `CustomCssTokensJson` (nullable), `Timezone`
- EF Core config + `administration` schema migration (new table `administration.tenant_settings`)
- `GetTenantSettingsQuery` + `UpdateTenantSettingsCommand` + handler + validator
- `GET /api/administration/settings` → `TenantSettingsDto`; `PUT /api/administration/settings` → 204
- BFF `/auth/me` response extended with `defaultPalette` field (read from `TenantSettings`)
- Angular `ThemeService.applyPaletteFromSession()` reads `defaultPalette` from `AuthService.currentUser()` signal and applies on login + settings save
- Angular `/administration/settings` page: display name input, timezone picker, palette selector with live preview chips (Purple / Indigo), BYOT CSS upload area
- BYOT sanitiser (C#): whitelist-only — accepts CSS text, extracts only `--stride-*` custom property assignments inside `:root {}`, strips everything else (no `url()`, `expression()`, `<script>`, `@import`)
- Preview mode: "Preview" button injects sanitised tokens into a `<style id="byot-preview">` tag in document `<head>` without saving; "Save" persists; "Reset" removes preview tokens

| ID | Story | Tasks | Complexity | Status |
|---|---|---|---|---|
| US-100 | As a developer, TenantSettings persists tenant configuration server-side | TenantSettings entity + ITenantSettingsRepository + EF Core config + administration schema migration | S | [ ] |
| US-101 | As a developer, tenant settings are read and updated via MediatR | GetTenantSettingsQuery + handler + TenantSettingsDto; UpdateTenantSettingsCommand + handler + validator | S | [ ] |
| US-102 | As a developer, tenant settings are exposed via API and BFF; /auth/me carries defaultPalette | TenantSettingsController (GET/PUT) + BFF proxy; extend MeResponse + BFF /auth/me with defaultPalette from TenantSettings lookup | S | [ ] |
| US-103 (was US-067) | As a tenant admin, my chosen default palette is applied automatically for all my users on login | Angular ThemeService.applyPaletteFromSession() reads defaultPalette from AuthService signal; applied in APP_INITIALIZER after /auth/me resolves | M | [ ] |
| US-104 (was US-068 + US-069) | As a tenant admin, I can upload a custom CSS token set (BYOT); tokens are sanitised server-side before storage | CssSanitiser service: extract only --stride-* properties in :root {}; reject url()/expression()/script; store sanitised JSON in TenantSettings.CustomCssTokensJson; return validation errors on bad input | L | [ ] |
| US-105 (was US-070) | As a tenant admin, I can preview my custom palette before saving it live | Angular preview mode: inject tokens into <style id="byot-preview">; Preview / Save / Reset buttons in settings page; preview cleared on navigation away | M | [ ] |

**Dependencies:** EP-035 (Administration module structure), Identity module (`MeResponse`, `/auth/me` endpoint)
**Risks:**
- Untrusted tenant CSS is a potential XSS vector — `CssSanitiser` must be deny-by-default: only emit known-safe `--stride-*` property assignments; never emit anything else
- Extending `/auth/me` adds a Dapper call to `TenantSettings` on every session check — cache in BFF Redis session or lazy-load on settings page

---

### EP-038 — Tenant Onboarding Flow

**Goal:** Self-service tenant registration wizard. A new customer creates their organisation, admin account, and first team invites in a single guided Angular flow — no manual provisioning required. Meets the **"Tenant onboarding flow complete"** architectural milestone.

**Acceptance Criteria:**
- `RegisterTenantCommand(orgName, slug, plan, adminEmail, adminPassword, adminDisplayName)` + handler: atomically creates `Tenant` + `User` (Admin role) + `TenantDomainMapping` (if corporate domain) + `UserTenantMapping`; returns JWT
- `Plan` enum added to `Tenant` entity: `Starter / Pro / Enterprise`
- `POST /api/identity/tenants/register` — unauthenticated; no `[Authorize]`; no `TenantMiddleware` interference (sets `TenantId` on context after creation, like `LoginCommand`)
- BFF `/bff/tenants/register` proxies to Host; auto-calls `/auth/login` to issue session cookie on success
- Angular `/onboarding` route — excluded from `authGuard`; 4-step stepper:
  1. **Organisation** — organisation name, slug (auto-generated, editable), plan tier (radio cards with feature bullets)
  2. **Admin Account** — display name, email, password (strength meter), confirm password
  3. **Appearance** — palette picker (Purple / Indigo), theme toggle (Light / Dark); live preview in stepper background
  4. **Invite Team** — optional: up to 5 email address inputs; sends invitations on completion
- On completion → auto-login → redirect to `/dashboard`
- Login page gains a "Create your workspace →" link pointing to `/onboarding`
- Slug uniqueness validated client-side (debounced API check) and server-side (unique constraint)

| ID | Story | Tasks | Complexity | Status |
|---|---|---|---|---|
| US-106 | As a new customer, I can register my organisation and first admin account in one command | RegisterTenantCommand + handler + validator; Plan enum on Tenant entity (non-breaking migration); slug uniqueness constraint | L | [ ] |
| US-107 | As a developer, tenant registration is accessible via unauthenticated API and BFF | TenantsController: POST /api/identity/tenants/register (no [Authorize]) + DTO; BFF TenantRegistrationApiClient + BFF proxy; auto-session after registration | S | [ ] |
| US-108 | As a new customer, I complete the 4-step onboarding wizard and land on the dashboard | Angular OnboardingPageComponent: 4-step stepper with form validation per step; appearance live preview; invite step calls InviteUserCommand for each email; auto-login on complete; "Create your workspace" link on login page | XL | [ ] |

**Dependencies:** EP-035 (InviteUserCommand for step 4), EP-037 (palette options + ThemeService)
**Risks:**
- `RegisterTenantCommand` handler must set `TenantId` on `ITenantContext` after Tenant creation — same bypass pattern as `LoginCommand` (sets context manually before downstream operations)
- Slug uniqueness must be enforced at the DB level (`UNIQUE INDEX` on `tenants.slug`) not just in the validator
- Onboarding route (`/onboarding`) must be listed as a `canActivate: []` exception in `app.routes.ts` to bypass the root `authGuard`

---

### EP-039 — SaaS Hardening

**Goal:** Protect the platform from API abuse with per-tenant rate limiting, and give tenant admins full visibility into sensitive operations via an audit trail.

**Acceptance Criteria:**
- Rate limiter: ASP.NET Core `AddRateLimiter` with a `TenantSlidingWindowPolicy` — 100 requests per 60-second window per `TenantId` (read from JWT `tid` claim); anonymous requests share a single global bucket; 429 response includes `Retry-After` header
- `AuditLog` entity in Administration domain: `Id`, `TenantId`, `ActorId` (UserId), `ActorEmail`, `Action` (string constant), `ResourceType`, `ResourceId` (nullable), `OldValueJson` (nullable), `NewValueJson` (nullable), `Timestamp`
- Audit events captured (via `IAuditLogger.LogAsync(...)` injected into command handlers): user invited, user role changed, user deactivated/reactivated, invoice generated/sent/paid/voided, tenant settings updated
- `GET /api/administration/audit-log?page=1&pageSize=50&from=&to=&action=` → paged `AuditLogEntryDto` list; Admin role only
- Angular `/administration/audit-log` page: table (timestamp, actor, action badge, resource, details expander), date range filter, action dropdown filter, skeleton, empty state

| ID | Story | Tasks | Complexity | Status |
|---|---|---|---|---|
| US-109 | As a SaaS operator, each tenant is rate-limited to prevent API abuse | AddRateLimiter in Host Program.cs; TenantSlidingWindowPolicy: reads tid claim, falls back to IP for anonymous; app.UseRateLimiter(); 429 + Retry-After | M | [ ] |
| US-110 | As a developer, sensitive admin actions are written to an audit log | AuditLog entity + EF config + administration schema migration; IAuditLogger interface + EF-backed AuditLogger; injected into InviteUser, UpdateUserRole, Deactivate, GenerateInvoice, MarkPaid, VoidInvoice, UpdateTenantSettings handlers | M | [ ] |
| US-111 | As a tenant admin, I can view the audit trail for my organisation | GetAuditLogQuery + Dapper read + handler; AuditLogController (GET) + BFF proxy + BFF AdminController extension; Angular /administration/audit-log page: table + date filter + action filter + skeleton | M | [ ] |

**Dependencies:** EP-035 (Administration module), EP-036 (invoice events to audit), EP-037 (settings events to audit)
**Risks:**
- `AuditLog` table grows unboundedly — add `INDEX ON (tenant_id, timestamp DESC)`; plan a TTL purge background job in Phase 7
- `IAuditLogger` must not block the command handler if audit write fails — fire-and-forget with structured logging fallback

---

### Sprint 6 Dependency Order

```
EP-035 (User Administration)   ← unblocked; start here
  ├─► EP-037 (Tenant Settings + BYOT)
  │     └─► EP-038 (Onboarding — needs palette + invite)
  └─► EP-039 (SaaS Hardening — audits admin actions)

EP-036 (Invoicing)             ← parallel with EP-037 after EP-035 done
  └─► EP-039 (audit captures invoice events)
```

---

### Sprint 6 Architectural Risks

| Risk | Severity | Mitigation |
|---|---|---|
| RegisterTenantCommand bypasses TenantMiddleware | HIGH | Handler sets TenantId manually on ITenantContext after Tenant creation (same pattern as LoginCommand) |
| BYOT custom CSS enabling XSS via custom properties | HIGH | CssSanitiser: deny-by-default, whitelist only `--stride-*` properties in `:root {}` |
| InviteUserCommand cross-tenant user creation | HIGH | Handler reads TenantId from ITenantContext (JWT), never from request body |
| AuditLog table growth | MEDIUM | Indexed on (tenant_id, timestamp); Phase 7 adds TTL purge job |
| Slug uniqueness race condition at registration | MEDIUM | DB UNIQUE INDEX enforces constraint; handler catches DbUpdateException and maps to validation error |
| /auth/me extended with TenantSettings lookup | LOW | Cache defaultPalette in Redis session payload; avoid per-request Dapper query |

---

### Sprint 6 Cross-Cutting Concerns

| Concern | Strategy |
|---|---|
| Rate limiting | ASP.NET Core built-in; tenant-aware policy reads `tid` claim |
| Audit trail | `IAuditLogger` interface in Application; EF-backed impl in Infrastructure; injected into relevant command handlers |
| BYOT sanitisation | Server-side C# whitelist parser; CSS never trusted from client |
| Tenant registration | Single `RegisterTenantCommand` creates Tenant + User + assigns Admin role atomically in a single EF transaction |
| Palette per tenant | `defaultPalette` stored in `TenantSettings`; propagated via `/auth/me`; applied by `ThemeService` on session bootstrap |
| Audit log read isolation | Dapper (not EF Core) for paged audit reads — consistent with ADR-005 |

---

## Sprint 7 — Portfolio & Deployment Polish (🔵 PLANNED)

**Phase:** Phase 7
**Status:** Issues created, board populated — ready to start
**GitHub milestone:** #7 — Phase 7 — Portfolio & Deployment Polish

### Test stack decision
**Backend:** xUnit (existing) + FluentAssertions v7 + NSubstitute v5 + Bogus v35 + TestContainers
**Angular:** jest-preset-angular (replaces Karma) + @testing-library/angular
**Why NSubstitute over Moq:** Moq's `Verify()` has known reliability gaps with `async` methods. NSubstitute's `Received()` works correctly for async mocking every time.

| Epic | GitHub # | Stories | Status |
|---|---|---|---|
| EP-040 Unit & Integration Tests | #195 | US-112 #200, US-113 #201, US-114 #202 | [ ] Backlog |
| EP-041 Refresh Token Flow | #196 | US-115 #203, US-116 #204, US-117 #205, US-118 #206 | [ ] Backlog |
| EP-042 Invite Email Flow | #197 | US-119 #207, US-120 #208, US-121 #209, US-122 #210 | [ ] Backlog |
| EP-043 Scheduling Module | #198 | US-123 #211, US-124 #212, US-125 #213, US-126 #214, US-127 #215 | [ ] Backlog |
| EP-044 Live Deployment — Dev Environment | #199 | US-128 #216, US-129 #217, US-130 #218, US-131 #219 | [ ] Backlog |
| EP-045 Tenant Custom RBAC | #245 | US-132 #248, US-133 #249, US-134 #250, US-135–140 | 🔵 In Progress — US-132+US-133 ✅ PR #358; US-134 ✅ PR #359; US-135 ✅ PR #360; US-136 ✅ PR #361; US-137 ✅ PR #362; US-138 ✅ PR #363; US-139 🔵 PR #364; US-140 ⏳ |

### Milestone #11 — Product Layer Gaps (EP-048–061, implemented as part of Phase 7)

| Epic | Stories | PRs | Status |
|---|---|---|---|
| EP-048 Step Ownership & Assignment | US-147–149 (#277–279) | (prior) | [x] Done |
| EP-049 Client/Customer Entity | US-150–152 (#280–282) | (prior) | [x] Done |
| EP-050 Workflow ↔ Invoice Integration | US-153–155 (#283–285) | (prior) | [x] Done |
| EP-051 File & Photo Attachments | US-156–158 (#286–288) | (prior) | [x] Done |
| EP-052 Advanced Step Types | US-159–161 (#289–291) | (prior) | [x] Done |
| EP-053 SLA & Deadline Tracking | US-162–164 (#292–294) | #317 #318 #319 | [x] Done |
| EP-054 Comments & Activity Log | US-165–167 (#295–297) | #320 #321 #322 | [x] Done |
| EP-055 Mobile-First Experience | US-168 (#298) / US-169 (#299) / US-170 (#300) | — | [ ] Next |
| EP-056 Approval Gates | US-171 (#301) / US-172 (#302) / US-173 (#303) | #333 #334 #335 | [x] Done — 2026-06-09 |
| EP-057 Actionable Dashboard | US-174 (#304) / US-175 (#305) / US-176 (#306) | #336 #337 #338 | [x] Done — 2026-06-12 |
| EP-058 External Customer-Facing Link | US-177 (#307) / US-178 (#308) / US-179 (#309) | #341 #342 #343 | [x] Done — US-177 ✅ US-178 ✅ US-179 PR #343 🔵 In Review |
| EP-059 Webhooks & Integrations | US-180 (#310) / US-181 (#311) / US-182 (#312) | #345 | [ ] In Progress — US-180 PR #345 🔵 In Review, US-181/182 ⏳ |
| EP-060–EP-061 | per board | — | [ ] Backlog |
| EP-062 Teams / Dept Entity (extra) | US-187 (#326) / US-188 (#328) / US-189 (#329) | #323 #324 #325 | [x] Done — 2026-06-09 |

### Dependency order
```
EP-040 (Tests)         — unblocked, start first
EP-041 (Refresh Token) — unblocked, parallel with tests
EP-042 (Invite Email)  — depends on IEmailSender (US-119) first
EP-043 (Scheduling)    — unblocked, parallel track
EP-044 (Deployment)    — last, needs everything else stable
```

---

## Architectural Milestones Tracker

| Milestone | Target Sprint | Status |
|---|---|---|
| Monorepo builds on .NET 10 | S1 | [x] Done |
| Frontend builds with Angular 21 + Tailwind 4 | S1 | [x] Done |
| Phase 1 committed to GitHub | S1 | [x] Done |
| Tenant-scoped login with Redis session | S2 | [x] Done (US-024) |
| RBAC claims authorization wired | S2 | [x] Done (US-029) |
| Angular auth guard + BFF session check | S2 | [x] Done — US-030 PR #70, US-031 PR #71 |
| Workflow domain model + state machine | S3 | [x] Done — EP-018 PR #104 |
| Workflow API layer (CRUD + lifecycle) | S3 | [x] Done — EP-021 PR #107 |
| Angular Workflow list + detail UI | S3 | [x] Done — US-054 PR #108, US-055 PR #110 |
| EF Core + Dapper split strategy proven | S4 | [x] Done — Phase 4 complete |
| All modules observable (Serilog + Seq + OTEL) | S5 | [x] Done — EP-034 PR #162 |
| Tenant onboarding flow complete | S6 | [x] Done — EP-038 |
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
| `IJwtTokenService` | Identity.Application | 2 | [x] Done (US-024) |
| `ISessionStore` (RedisTicketStore) | Identity.Infrastructure / STRIDE.BFF | 2 | [x] Done (US-024) |
| `ITenantResolver` | BuildingBlocks.Infrastructure | 1 (interface), 2 (impl) | [x] Done (US-021) |
| `IPasswordHasher` | Identity.Application | 2 | [x] Done (US-026, PBKDF2-SHA256) |
| `IUserRepository` | Identity.Application | 2 | [x] Done (US-023) |
| `ITenantRepository` | Identity.Application | 2 | [x] Done (US-023) |
| `ITenantContextSetter` | BuildingBlocks.Application | 2 | [x] Done (US-026) |
| `ICurrentUser` (CurrentUser impl) | BuildingBlocks.Infrastructure | 2 | [x] Done (US-028) |
