# STRIDE — Module Interaction Diagram

**Story:** US-037 | **Epic:** EP-017 | **Milestone:** Docs and Diagrams
**Trigger:** End of Phase 1 (retroactive — scaffold complete as of 2026-05-14)

> This diagram shows how the internal layers of the STRIDE Host relate to one another:
> the shared BuildingBlocks kernel, the seven feature modules, the MediatR pipeline behaviours,
> and the cross-module domain event flows.
> For the full login sequence, see the [Auth Flow Diagram (US-038)](auth-flow.md).

---

```mermaid
flowchart TB
    classDef building  fill:#374151,stroke:#1F2937,color:#fff,font-size:13px
    classDef module    fill:#1168BD,stroke:#0B4884,color:#fff,font-size:13px
    classDef xcut      fill:#6B21A8,stroke:#4C1D95,color:#fff,font-size:12px
    classDef host      fill:#0F766E,stroke:#0D5C54,color:#fff,font-size:13px
    classDef event     fill:#92400E,stroke:#78350F,color:#fff,font-size:12px

    subgraph BB["🧱  BuildingBlocks  (shared kernel — referenced by every module)"]
        direction LR
        BBD("**Domain**\n─────────────\nIDomainEvent\nAuditableEntity\nValueObject‹T›\nEntity‹TId›")
        BBA("**Application**\n─────────────\nResult‹T›\nIEventBus\nIMediator\nITenantContext\nICurrentUser\nIBackgroundTaskQueue")
        BBI("**Infrastructure**\n─────────────\nTenantAwareRepository‹T,TCtx›\nITenantResolver\nBaseDbContext\nMigrationHelpers")
    end

    subgraph MODS["🔷  Feature Modules  (each module owns: Domain / Application / Infrastructure / API)"]
        direction LR
        ID("**Identity**\n─────────────\nUser · Role · Permission\nLoginCommand · RegisterUser\nJwtTokenService\nTenantResolver")
        WF("**Workflows**\n─────────────\nWorkflowDefinition\nWorkflowInstance\nStep · StepInstance\nState machine (FSM)")
        SC("**Scheduling**\n─────────────\nJob triggers\nCron expressions\n(Phase 5)")
        RP("**Reporting**\n─────────────\nKPI aggregation\nDapper read models\nCSV export")
        NT("**Notifications**\n─────────────\nEmail · in-app alerts\nTemplated dispatch\n(Phase 5)")
        IN("**Invoicing**\n─────────────\nInvoice generation\nFinance reporting\n(Phase 6)")
        AD("**Administration**\n─────────────\nTenant management\nRole seeding\nSystem config")
    end

    subgraph PIPE["⚙️  MediatR Pipeline  (STRIDE.Host — registered globally)"]
        direction LR
        VAL("**ValidationBehaviour**\n─────────────\nFluentValidation\nauto-discovers validators\nreturns Result.Failure")
        LOG("**LoggingBehaviour**\n─────────────\nSerilog structured\nCorrelationId + TenantId\non every command/query")
        TEN("**TenantBehaviour**\n─────────────\nSets ITenantContext\nfrom cookie claims\nbefore handler runs")
    end

    EVBUS("**IEventBus**\n─────────────\nMediatR in-process\n(Phases 1–3)\nReplaced by message\nbroker in Phase 6+")

    HOST("🔷  STRIDE.Host\n─────────────\nComposition root\nDI registration\nModule wiring\nHealth checks\nSerilog + OpenTelemetry")

    BB --> MODS
    BB --> PIPE
    MODS --> PIPE
    PIPE --> HOST

    ID -. "UserCreatedEvent\n[IEventBus]" .-> NT
    ID -. "RoleAssignedEvent\n[IEventBus]" .-> AD
    WF -. "WorkflowStartedEvent\nStepCompletedEvent\n[IEventBus]" .-> NT
    WF -. "WorkflowData\n[Dapper read model]" .-> RP

    class BBD,BBA,BBI building
    class ID,WF,SC,RP,NT,IN,AD module
    class VAL,LOG,TEN,EVBUS xcut
    class HOST host
```

---

## Key Design Decisions Reflected

| Decision | What the diagram shows |
|---|---|
| Modular monolith (ADR-001) | Seven feature modules in a single Host process — each module owns its full vertical slice |
| Shared kernel (BuildingBlocks) | Every module references BuildingBlocks; no module references another module directly |
| MediatR pipeline (ADR-003) | All commands and queries flow through Validation → Logging → TenantBehaviour before reaching handlers |
| In-process event bus (Phases 1–3) | Domain events flow via `IEventBus` (MediatR `Publish`) — note indicates Phase 6+ replacement |
| TenantAwareRepository (ADR-006) | BBI layer: `TenantAwareRepository<T,TCtx>` auto-applies `WHERE TenantId = @tid` on every query |
| Dapper for read models | Reporting module uses Dapper directly — bypasses EF change tracking for high-performance aggregations |
| Result‹T› pattern | Application layer: no exceptions for domain errors — all handlers return `Result<T>` |

---

## BuildingBlocks Layers

| Layer | Key Types | Purpose |
|---|---|---|
| Domain | `IDomainEvent`, `AuditableEntity`, `ValueObject<T>`, `Entity<TId>` | Primitive building blocks for all domain entities and events |
| Application | `Result<T>`, `IEventBus`, `IMediator`, `ITenantContext`, `ICurrentUser`, `IBackgroundTaskQueue` | Cross-cutting application-layer contracts — no infrastructure dependencies |
| Infrastructure | `TenantAwareRepository<T,TCtx>`, `ITenantResolver`, `BaseDbContext`, `MigrationHelpers` | Shared EF Core base classes; multi-tenant filtering applied once, inherited everywhere |

---

## Domain Event Flows

| Producer | Event | Consumer | Trigger |
|---|---|---|---|
| Identity | `UserCreatedEvent` | Notifications | Welcome email on first registration |
| Identity | `RoleAssignedEvent` | Administration | Audit log entry on role change |
| Workflows | `WorkflowStartedEvent` | Notifications | Alert assigned users on workflow activation |
| Workflows | `StepCompletedEvent` | Notifications | Alert next assignee when a step is completed |
| Workflows | Dapper read model query | Reporting | KPI aggregation over workflow data |

---

## MediatR Pipeline Order

Every command and query registered in the Host passes through behaviours in this order:

1. **TenantBehaviour** — Resolves `ITenantContext` from the session cookie claims and sets `TenantId` before any handler logic runs. All downstream `TenantAwareRepository<T>` calls are automatically scoped.
2. **ValidationBehaviour** — Runs all FluentValidation validators auto-discovered for the request type. Returns `Result.Failure` with a validation error list without invoking the handler.
3. **LoggingBehaviour** — Logs the command/query name, `CorrelationId`, and `TenantId` via Serilog structured logging. Measures handler duration.

---

*Source file: [`module-interaction.mmd`](module-interaction.mmd) | PNG: [`module-interaction.png`](module-interaction.png)*
*Last updated: 2026-05-16 | Next update trigger: new module added or IEventBus replaced with message broker*
