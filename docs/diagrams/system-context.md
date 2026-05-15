# STRIDE — System Context Diagram (C4 Level 1)

**Story:** US-035 | **Epic:** EP-017 | **Milestone:** Docs and Diagrams
**Trigger:** End of Phase 1 (retroactive — scaffold complete as of 2026-05-14)

> This diagram shows the top-level boundary of the STRIDE platform, the human users who interact with it,
> and the external systems it depends on or integrates with. Internal module detail is deferred to the
> [Container Diagram (US-036)](container.md).

---

```mermaid
flowchart LR
    classDef person    fill:#08427B,stroke:#052E56,color:#fff,font-size:13px
    classDef system    fill:#1168BD,stroke:#0B4884,color:#fff,font-size:13px
    classDef extSystem fill:#999999,stroke:#6b6b6b,color:#fff,font-size:13px
    classDef extDb     fill:#999999,stroke:#6b6b6b,color:#fff,font-size:13px

    subgraph USERS["👤  Users"]
        direction TB
        TA("**Tenant Admin**\n─────────────\nManages users, roles,\nand tenant config.")
        OM("**Operations Manager**\n─────────────\nCreates workflows\nand schedules.")
        FW("**Field Worker**\n─────────────\nExecutes tasks\nand logs progress.")
        FU("**Finance User**\n─────────────\nReviews invoices\nand reports.")
    end

    subgraph STRIDE["🔷  STRIDE Platform"]
        direction TB
        SPA("**Angular SPA**\n─────────────\nRuns in the browser.\nNo bearer tokens ever\nreached the client.")
        BFF("**STRIDE BFF**\n─────────────\nBackend-for-Frontend.\nIssues HttpOnly cookies.\nProxies SPA → Host.")
        HOST("**STRIDE Host**\n─────────────\nModular monolith API.\nIdentity · Workflows\nScheduling · Reporting\nNotifications · Invoicing\nAdministration")
    end

    subgraph EXT["☁️  External Systems & Data Stores"]
        direction TB
        SQL[("**Azure SQL**\n─────────────\nPersistent storage.\nSchema-separated:\nidentity.* · workflows.*\netc.")]
        REDIS[("**Redis**\n─────────────\nSession store + cache.\nKey pattern:\ntenant:{id}:session:{id}")]
        MAIL("**Email Service**\n─────────────\nTransactional email.\n(Phase 5 — Notifications)")
        IDP("**External IdP**\n─────────────\nAzure AD / Auth0 / OIDC.\nFuture SSO federation.\n(Phase 6+)")
    end

    TA -- "HTTPS / Browser" --> SPA
    OM -- "HTTPS / Browser" --> SPA
    FW -- "HTTPS / Browser" --> SPA
    FU -- "HTTPS / Browser" --> SPA

    SPA -- "HTTPS +\nHttpOnly Cookie" --> BFF
    BFF -- "HTTPS /\nTyped HttpClient" --> HOST

    BFF -- "Session store\nRedis / TLS" --> REDIS
    HOST -- "EF Core + Dapper\n/ TLS" --> SQL
    HOST -- "Cache & revoke\nRedis / TLS" --> REDIS
    HOST -. "SMTP / HTTP\n(Phase 5)" .-> MAIL
    HOST -. "OIDC / SAML\n(Phase 6+)" .-> IDP

    class TA,OM,FW,FU person
    class SPA,BFF,HOST system
    class MAIL,IDP extSystem
    class SQL,REDIS extDb
```

---

## Key Design Decisions Reflected

| Decision | What the diagram shows |
|---|---|
| BFF pattern (ADR-007, ADR-008) | Angular SPA only talks to BFF — never directly to Host |
| HttpOnly cookie auth | Relationship label: "HTTPS + HttpOnly Cookie" — no token in browser |
| Modular monolith (ADR-001) | Host is a single deployable boundary containing all 7 modules |
| Shared DB + TenantId isolation (ADR-006) | One Azure SQL instance, schema-separated — not per-tenant databases |
| Redis session store (ADR-007) | BFF and Host both connect — BFF stores sessions, Host can revoke |
| Future-ready IdP (Auth Architecture) | External IdP shown as Phase 6+ dashed integration |
| Schema separation (ADR-012) | Azure SQL note: `identity.*, workflows.*, etc.` |

---

## Actors

| Actor | Role in STRIDE |
|---|---|
| Tenant Admin | Full platform admin within their tenant — manages users, roles, tenant settings |
| Operations Manager | Creates/manages workflows, assigns tasks, oversees scheduling |
| Field Worker | Executes tasks in the field; logs status updates and completions |
| Finance User | Read-only access to invoicing and financial reports |

---

*Source file: [`system-context.mmd`](system-context.mmd) | PNG: [`system-context.png`](system-context.png)*
*Last updated: 2026-05-15 | Next update trigger: Phase 2 completion (add auth detail → Container Diagram)*
