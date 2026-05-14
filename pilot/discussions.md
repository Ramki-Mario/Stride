# STRIDE Discussions & Finalized Decisions

---

# Architectural Decisions Record

## ADR-001: Modular Monolith Architecture

Decision: Use modular monolith as the initial architecture.
Structure: BuildingBlocks / Modules / Host / BFF / Gateway.
Each module contains: API / Application / Domain / Infrastructure layers.
Future: extraction-ready for independent services.
Status: Finalized

---

## ADR-002: BFF is a Dedicated Project (STRIDE.BFF)

Decision: BFF is its own ASP.NET Core project, not middleware inside Host.

Responsibilities:
- session handling
- auth orchestration
- HttpOnly cookie issuance
- frontend aggregation/proxying

Rules:
- Angular calls ONLY the BFF
- BFF does NOT contain business logic
- BFF forwards authenticated requests to STRIDE.Host

Status: Finalized

---

## ADR-003: MediatR for Commands, Queries, and Domain Events

Decision: Use MediatR pragmatically for:
- Commands (write operations)
- Queries (read operations within a module)
- Domain event dispatch

Do NOT use for cross-module communication — use IEventBus.
Do NOT implement full CQRS/event sourcing.
Status: Finalized

---

## ADR-004: Queue Abstractions Scaffolded in Phase 1 (Interfaces Only)

Decision: Scaffold the following interfaces in BuildingBlocks during Phase 1:
- IDomainEvent (BuildingBlocks.Domain)
- IIntegrationEvent (BuildingBlocks.Domain)
- IEventBus (BuildingBlocks.Application)
- IBackgroundTaskQueue (BuildingBlocks.Application)

No concrete implementations yet.
Implementation deferred to Phase 3+.
Status: Finalized

---

## ADR-005: EF Core for Writes, Dapper for Reporting Reads

Decision: Split ORM usage by concern.
EF Core: transactional writes, aggregate persistence, domain operations, migrations.
Dapper: reporting queries, dashboard projections, aggregated read models.
Status: Finalized

---

## Finalized Backend Structure Decisions

- Modular Clean Architecture
- Internal domain events
- Queue abstraction (interfaces only)

---

## Finalized Data Decisions

- Azure SQL
- Shared DB + TenantId isolation
- EF Core + Dapper split
- Redis integration
- Mandatory columns: Id, TenantId, CreatedAt, UpdatedAt, CreatedBy, IsDeleted
- TenantId-first composite indexes

---

## Finalized Authentication Decisions

Current:
- Internal auth
- BFF architecture (STRIDE.BFF dedicated project)
- HttpOnly cookies
- Redis session store

Future:
- Azure AD
- Auth0
- Okta
- OIDC/SAML-ready

---

## Finalized Scalability Decisions

- Modular monolith initially
- Future module extraction capability
- Evolutionary architecture
- Shard-aware design

---

## Finalized Deployment Decisions

- Azure-native
- Docker-ready (Docker Compose for local dev)
- CI/CD-ready
- Observability-first

---

## Finalized AI Engineering Decisions

- AI-assisted development mandatory
- Dedicated agent files (populated before Phase 1 implementation)
- Persistent architecture memory
- Strict AI governance
- Incremental implementation
- Avoid uncontrolled generation

---

## ADR-011: BFF ↔ Host Communication via Typed HttpClient

Decision: STRIDE.BFF communicates with STRIDE.Host using typed HttpClient clients.
Each backend module surface the BFF needs is wrapped in a typed client class (e.g., `IdentityApiClient`, `WorkflowApiClient`).
YARP is intentionally deferred — not needed in Phase 1/2.
Status: Finalized (2026-05-14)

---

## ADR-012: SQL Schema Separation Per Module

Decision: Each module owns a dedicated SQL schema.
Examples: `identity.Users`, `workflows.Tasks`, `scheduling.Assignments`.
Benefits: logical isolation within shared DB, extraction-ready without rename migrations, clear module boundaries at DB level.
Each module owns: schema name, DbContext, migrations.
Status: Finalized (2026-05-14)

---

## ADR-013: Custom Result<T> in BuildingBlocks

Decision: Use a custom `Result<T>` implementation inside BuildingBlocks (e.g., `STRIDE.BuildingBlocks.Application` or a `STRIDE.BuildingBlocks.Shared` project).
No external NuGet packages (FluentResults, ErrorOr, OneOf, etc.).
All Application layer command/query handlers return `Result<T>` or `Result`.
Status: Finalized (2026-05-14)

---

## ADR-014: In-Process MediatR-Backed IEventBus for Phase 1–3

Decision: Scaffold a lightweight in-process `IEventBus` implementation backed by MediatR `IPublisher`.
This allows domain events and integration events to flow in Phase 1–3 without external queue infrastructure.
Azure Service Bus implementation of `IEventBus` is deferred to Phase 6+.
The interface contract (`IEventBus`) is unchanged — swapping the implementation requires no Application layer changes.
Status: Finalized (2026-05-14)

---

## ADR-015: Angular Standalone Components Architecture

Decision: Use Angular standalone components (no NgModules).
Feature organization remains feature-based (per folder structure in frontend.agent.md).
State management: Angular Signals for local/feature state + RxJS for HTTP streams and async events.
NgRx not introduced unless complexity demands it.
Status: Finalized (2026-05-14)

---

## Portfolio Positioning

Initial MVP:
Portfolio-grade enterprise architecture showcase

Long-Term Direction:
Real SaaS-ready operational platform
