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
| Tenant-scoped login with Redis session | S2 | [x] Done (US-024) |
| RBAC claims authorization wired | S2 | [x] Done (US-029) |
| Angular auth guard + BFF session check | S2 | [x] Done — US-030 PR #70, US-031 PR #71 |
| Workflow domain model + state machine | S3 | [x] Done — EP-018 PR #104 |
| Workflow API layer (CRUD + lifecycle) | S3 | [x] Done — EP-021 PR #107 |
| Angular Workflow list + detail UI | S3 | [x] Done — US-054 PR #108, US-055 PR #110 |
| EF Core + Dapper split strategy proven | S4 | [x] Done — Phase 4 complete |
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
| `IJwtTokenService` | Identity.Application | 2 | [x] Done (US-024) |
| `ISessionStore` (RedisTicketStore) | Identity.Infrastructure / STRIDE.BFF | 2 | [x] Done (US-024) |
| `ITenantResolver` | BuildingBlocks.Infrastructure | 1 (interface), 2 (impl) | [x] Done (US-021) |
| `IPasswordHasher` | Identity.Application | 2 | [x] Done (US-026, PBKDF2-SHA256) |
| `IUserRepository` | Identity.Application | 2 | [x] Done (US-023) |
| `ITenantRepository` | Identity.Application | 2 | [x] Done (US-023) |
| `ITenantContextSetter` | BuildingBlocks.Application | 2 | [x] Done (US-026) |
| `ICurrentUser` (CurrentUser impl) | BuildingBlocks.Infrastructure | 2 | [x] Done (US-028) |
