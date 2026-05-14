# project-rules.agent.md
# STRIDE — Master Project Rules Agent

## Purpose
Persistent AI memory for master engineering governance.
Read this file FIRST before any implementation session.
These rules are non-negotiable.

---

## The 10 Non-Negotiable Rules

1. **Tenant isolation is absolute.** Every query filters TenantId. No exceptions.
2. **Frontend never holds tokens.** BFF manages all auth. HttpOnly cookies only.
3. **Controllers are thin.** Inject IMediator only. Dispatch and return.
4. **Modules do not reference each other.** Cross-module via IEventBus only.
5. **DbContext never leaves Infrastructure.** Not in Application. Not in API.
6. **All async I/O passes CancellationToken.** No blocking calls.
7. **Every table is soft-deleted.** Never hard-delete tenant records.
8. **Redis keys are tenant-prefixed.** Never a global cache for tenant data.
9. **Build incrementally.** No speculative abstractions beyond current phase scope.
10. **Update current-status.md** after every significant implementation milestone.

---

## Pre-Implementation Checklist

Before writing any code for a module, confirm:
- [ ] Read architecture.agent.md
- [ ] Read backend.agent.md
- [ ] Read relevant domain agent (auth, workflows, database, etc.)
- [ ] Read coding-standards.agent.md
- [ ] Confirmed which phase this work belongs to
- [ ] Confirmed module boundaries are respected
- [ ] Confirmed TenantId isolation plan for this module
- [ ] Confirmed logging plan (TenantId, CorrelationId in scope)

---

## Phase Status Reference

| Phase | Name | Status |
|---|---|---|
| 0 | AI Engineering Workspace | Complete |
| 1 | Monorepo & Foundation | Pending |
| 2 | Identity & Tenant Foundation | Pending |
| 3 | Core Workflow Engine | Pending |
| 4 | Dashboard & Reporting | Pending |
| 5 | Notifications & Observability | Pending |
| 6 | SaaS Readiness | Pending |
| 7 | Portfolio & Deployment Polish | Pending |

---

## Architectural Decision Log Summary

| ID | Decision | Status |
|---|---|---|
| ADR-001 | Modular Monolith architecture | Finalized |
| ADR-002 | BFF is a dedicated STRIDE.BFF project | Finalized |
| ADR-003 | MediatR for commands, queries, domain events | Finalized |
| ADR-004 | IEventBus / IDomainEvent / IIntegrationEvent / IBackgroundTaskQueue scaffolded in Phase 1 | Finalized |
| ADR-005 | EF Core for writes, Dapper for reporting reads | Finalized |
| ADR-006 | Shared DB + TenantId isolation (shard-aware) | Finalized |
| ADR-007 | HttpOnly cookies + Redis sessions — no localStorage tokens | Finalized |
| ADR-008 | Angular SPA + BFF — no direct module API calls from frontend | Finalized |
| ADR-009 | Soft-delete on all tenant records | Finalized |
| ADR-010 | Module structure: BuildingBlocks / Modules / Host / BFF / Gateway | Finalized |
| ADR-011 | BFF ↔ Host via typed HttpClient; YARP deferred | Finalized |
| ADR-012 | SQL schema per module (identity.*, workflows.*, etc.) | Finalized |
| ADR-013 | Custom Result<T> in BuildingBlocks — no external NuGet | Finalized |
| ADR-014 | MediatR-backed in-process IEventBus for Phase 1–3 | Finalized |
| ADR-015 | Angular standalone components; Signals + RxJS hybrid state | Finalized |

---

## What Is Out of Scope (Phase 1)

Do NOT implement:
- Event Bus concrete implementation (IEventBus interface only)
- Background task queue implementation
- Kubernetes / AKS deployment
- Azure Service Bus
- External IdP (Azure AD, Auth0, Okta)
- Full CQRS/Event Sourcing
- Multi-region or dedicated tenant DB routing
- Billing / usage metering
- AI operational assistant
- Mobile applications

---

## Overengineering Triggers (Stop and Check)

If you find yourself about to:
- Create a new abstraction layer not in the architecture
- Add a new NuGet package for something already available
- Split a module into sub-modules prematurely
- Create a generic framework for something used once
- Add infrastructure for a future phase

...STOP. Re-read this file and the architecture docs. Check if it's in scope.

---

## AI Session Protocol

Start of every session:
1. Read current-status.md
2. Identify which phase and module is being worked on
3. Read relevant agent files for that module
4. Confirm scope before writing code

End of every significant session:
1. Update current-status.md
2. Note what was implemented
3. Note what is next
