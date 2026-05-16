# STRIDE — Tenant Resolution Diagram

**Story:** US-039 | **Epic:** EP-017 | **Milestone:** Docs and Diagrams
**Trigger:** End of Phase 1 (retroactive — scaffold complete as of 2026-05-14)

> This diagram expands Step 3 of the [Auth Flow Diagram (US-038)](auth-flow.md) —
> the `TenantResolver.ResolveAsync(email)` call inside the STRIDE Host.
> It shows how the system determines which tenant a user belongs to
> before credential validation begins.

---

```mermaid
flowchart TD
    classDef startEnd  fill:#08427B,stroke:#052E56,color:#fff,font-size:13px
    classDef process   fill:#1168BD,stroke:#0B4884,color:#fff,font-size:13px
    classDef decision  fill:#B45309,stroke:#92400E,color:#fff,font-size:13px
    classDef success   fill:#047857,stroke:#065F46,color:#fff,font-size:13px
    classDef error     fill:#B91C1C,stroke:#7F1D1D,color:#fff,font-size:13px
    classDef note      fill:#1E1B4B,stroke:#312E81,color:#fff,font-size:12px

    START(["Login Request arrives at\nLoginCommandHandler\n{ email: user@company.com }"])

    EXTRACT["TenantResolver.ResolveAsync(email)\n─────────────────────────────────\nExtract email domain:\n'user@company.com' → 'company.com'"]

    CHECK1{"Query: TenantDomainMappings\nWHERE Domain = 'company.com'\nAND IsDeleted = false"}

    CHECK2{"Query: UserTenantMappings\nJOIN Users ON UserId\nWHERE Users.Email = 'user@company.com'\nAND IsDeleted = false"}

    SET["ITenantContextSetter.SetTenantId(tenantId)\n─────────────────────────────────\nTenant scope established.\nAll downstream TenantAwareRepository‹T›\ncalls auto-filter by this TenantId."]

    PROCEED(["LoginCommandHandler continues:\nPBKDF2-SHA256 credential check\nJWT issued with tid = tenantId\n(sub · email · tid · roles[])"])

    ERR(["401 Unauthorized\n─────────────\nResult.Failure: 'No tenant found\nfor this email address.'\nLogin blocked."])

    START --> EXTRACT
    EXTRACT --> CHECK1
    CHECK1 -->|"✅  Row found\nCorporate domain registered\nto a tenant"| SET
    CHECK1 -->|"❌  No match\nGeneric email domain\n(gmail · hotmail · etc.)"| CHECK2
    CHECK2 -->|"✅  Row found\nUser explicitly mapped\nto a tenant"| SET
    CHECK2 -->|"❌  No mapping\nUser not registered\nwith any tenant"| ERR
    SET --> PROCEED

    NOTE1["📌  TenantRepository bypasses\nTenantAwareRepository filter.\nIt is a self-owning root —\nresolving Tenant cannot itself\nrequire a Tenant to be set."]

    NOTE2["📌  Shared DB strategy (ADR-006):\nEvery subsequent query uses\nTenantId as a mandatory filter.\nTenantAwareRepository‹T,TCtx›\napplies WHERE TenantId = @tid\nAND IsDeleted = false automatically."]

    CHECK1 -. "uses" .-> NOTE1
    SET -. "enables" .-> NOTE2

    class START startEnd
    class EXTRACT,SET process
    class CHECK1,CHECK2 decision
    class PROCEED success
    class ERR error
    class NOTE1,NOTE2 note
```

---

## Key Design Decisions Reflected

| Decision | What the diagram shows |
|---|---|
| Two-path tenant resolution | Domain lookup first (fast, covers corporate accounts); user-level mapping as fallback (covers personal/generic email domains) |
| Soft-delete filtering | Both queries include `AND IsDeleted = false` — deactivated domain mappings stop working immediately |
| TenantRepository bootstrap exception (ADR-006) | `TenantRepository` does **not** extend `TenantAwareRepository<T>` — it would be circular; it queries the `Tenants` table directly |
| No tenant = no login | If neither path resolves a tenant, the request fails with `401` before credentials are ever checked |
| `ITenantContextSetter` | Setting the TenantId on the ambient context automatically scopes all downstream `TenantAwareRepository<T>` queries — no need to pass TenantId as a parameter through every call |
| Shared DB isolation (ADR-006) | Once TenantId is set, `TenantAwareRepository<T,TCtx>` appends `WHERE TenantId = @tid AND IsDeleted = false` to every query automatically |

---

## Resolution Paths

### Path 1 — Corporate Domain Lookup
Used when the user's email domain is registered to a tenant (e.g. `@acme-corp.com`).

1. Extract domain from email: `user@acme-corp.com` → `acme-corp.com`
2. Query `TenantDomainMappings WHERE Domain = 'acme-corp.com' AND IsDeleted = false`
3. If a row is found → tenant identified; proceed to `ITenantContextSetter.SetTenantId`

### Path 2 — Explicit User Mapping (Fallback)
Used when the domain is a generic provider (e.g. `@gmail.com`, `@hotmail.com`) that is not registered as a corporate domain.

1. Query `UserTenantMappings JOIN Users ON UserId WHERE Users.Email = 'user@gmail.com' AND IsDeleted = false`
2. If a row is found → tenant identified; proceed to `ITenantContextSetter.SetTenantId`
3. If no row is found → `Result.Failure("No tenant found for this email address.")` → `401 Unauthorized`

---

## Why TenantRepository Is Special

Every other repository in STRIDE extends `TenantAwareRepository<T, TCtx>`, which automatically applies:
```sql
WHERE TenantId = @currentTenantId AND IsDeleted = false
```
`TenantRepository` **cannot** do this — it is the mechanism that *establishes* the TenantId in the first place.
If it required a TenantId to query, it would be an unresolvable bootstrap dependency.

`TenantRepository` therefore queries the `Tenants` table directly, with no ambient tenant filter.
All other repositories safely assume the filter is already applied.

---

*Source file: [`tenant-resolution.mmd`](tenant-resolution.mmd) | PNG: [`tenant-resolution.png`](tenant-resolution.png)*
*Last updated: 2026-05-16 | Next update trigger: external IdP tenant federation added (Phase 6+)*
