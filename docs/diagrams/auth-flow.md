# STRIDE — Authentication Flow Diagram

**Story:** US-038 | **Epic:** EP-017 | **Milestone:** Docs and Diagrams
**Trigger:** End of Phase 1 (retroactive — scaffold complete as of 2026-05-14)

> This diagram traces the full login sequence from browser to BFF to Host and back.
> It explains where the JWT lives, why the browser never sees it, and how the opaque session
> cookie is constructed. The tenant resolution step is expanded in the
> [Tenant Resolution Diagram (US-039)](tenant-resolution.md).

---

```mermaid
flowchart TD
    classDef actor    fill:#08427B,stroke:#052E56,color:#fff,font-size:13px
    classDef bff      fill:#1168BD,stroke:#0B4884,color:#fff,font-size:13px
    classDef host     fill:#0F766E,stroke:#0D5C54,color:#fff,font-size:13px
    classDef store    fill:#555555,stroke:#333333,color:#fff,font-size:12px
    classDef rule     fill:#1E1B4B,stroke:#312E81,color:#fff,font-size:12px

    BROWSER(["🌐  Angular SPA\n(Browser)"])

    subgraph BFFBOX["🔷  STRIDE BFF  ·  ASP.NET Core (.NET 10)"]
        direction TB
        B1["**Step 1 — Receive login request**\nPOST /bff/auth/login\n{ email, password }"]
        B2["**Step 6 — Receive JWT from Host**\nHS256 token · sub · email · tid · roles[]\nNever forwarded to browser"]
        B3["**Step 7 — Store claims in Redis**\nKey: tenant:{tenantId}:session:{opaqueId}\nTTL = JWT expiry"]
        B4["**Step 8 — Issue session cookie**\nSet-Cookie: session={tid}.{opaqueId}\nHttpOnly · SameSite=Strict · Secure\nOpaque value — not the JWT"]
    end

    subgraph HOSTBOX["🔷  STRIDE Host  ·  Modular Monolith (.NET 10)"]
        direction TB
        H1["**Step 2 — Receive internal login call**\nPOST /api/identity/auth/login\nInternal network only — not publicly routable"]
        H2["**Step 3 — Resolve tenant**\nTenantResolver.ResolveAsync(email)\nExtract domain → TenantDomainMappings\nor UserTenantMappings fallback"]
        H3["**Step 4 — Set tenant context**\nITenantContextSetter.SetTenantId(tenantId)\nAll downstream repository calls are\nautomatically tenant-scoped"]
        H4["**Step 5 — Validate credentials**\nLoginCommandHandler\nPBKDF2-SHA256 hash compare\nIssue HS256 JWT on success"]
    end

    REDIS[("Redis\ntenant:{id}:session:{id}")]

    BROWSER -->|"POST /bff/auth/login\n{ email, password }"| B1
    B1 -->|"Forward to Host\n(internal network)"| H1
    H1 --> H2
    H2 --> H3
    H3 --> H4
    H4 -->|"JWT (HS256)\nsub · email · tid · roles[]"| B2
    B2 --> B3
    B3 -->|"Write session claims"| REDIS
    B3 --> B4
    B4 -->|"HttpOnly cookie\n(no JWT in response body)"| BROWSER

    BROWSER -->|"All subsequent requests:\ncookie auto-attached by browser"| B1

    RULE["🔒  Security constraint (ADR-007 / ADR-008)\n─────────────────────────────────────────\nJWT is internal to BFF ↔ Host only.\nThe browser never sees the JWT.\nCookie value is an opaque composite:\n  tid.opaqueSessionId\nClaims are resolved server-side from\nRedis on every authenticated request."]

    B2 -. "enforces" .-> RULE

    class BROWSER actor
    class B1,B2,B3,B4 bff
    class H1,H2,H3,H4 host
    class REDIS store
    class RULE rule
```

---

## Key Design Decisions Reflected

| Decision | What the diagram shows |
|---|---|
| BFF pattern (ADR-007) | Host API endpoint is internal-only; SPA has no direct route to it |
| JWT never reaches browser (ADR-008) | B2 receives the JWT; B4 sends only an opaque cookie — no `Authorization` header, no `localStorage` |
| Opaque session cookie | Cookie value `{tid}.{opaqueSessionId}` — not decodable by the browser; claims live in Redis |
| Redis session store | B3 writes `tenant:{tenantId}:session:{opaqueId}` → full claims JSON; TTL matches JWT expiry |
| HS256 symmetric JWT (internal) | JWT is signed with a shared secret — used only for BFF↔Host trust, never exposed externally |
| PBKDF2-SHA256 password hashing | H4 compares the stored hash — no plaintext passwords at rest |
| Cookie flags | `HttpOnly` (no JS access) · `SameSite=Strict` (no CSRF) · `Secure` (HTTPS only) |

---

## Login Flow — Step by Step

| Step | Actor | Action |
|---|---|---|
| 1 | BFF | Receives `POST /bff/auth/login { email, password }` from Angular SPA |
| 2 | Host | Receives forwarded login request on internal-only `POST /api/identity/auth/login` |
| 3 | Host — Identity | `TenantResolver.ResolveAsync(email)` — extracts domain, queries `TenantDomainMappings`, falls back to `UserTenantMappings` |
| 4 | Host — Identity | `ITenantContextSetter.SetTenantId(tenantId)` — all downstream DB calls now auto-filter by this TenantId |
| 5 | Host — Identity | `LoginCommandHandler` — PBKDF2-SHA256 credential check; issues HS256 JWT on success |
| 6 | BFF | Receives JWT (`sub`, `email`, `tid`, `roles[]`); JWT is never forwarded to the browser |
| 7 | BFF | Writes decoded claims to Redis at `tenant:{tenantId}:session:{opaqueId}`, TTL = JWT expiry |
| 8 | BFF | Issues `Set-Cookie: session={tid}.{opaqueId}` with `HttpOnly`, `SameSite=Strict`, `Secure` flags |
| — | Browser | Stores cookie; auto-attaches it on every subsequent HTTPS request to the BFF |

---

## Subsequent Request Handling

On every authenticated request after login:

1. Browser auto-attaches the `session` cookie (no JS involvement — `HttpOnly`).
2. BFF middleware parses `tid.opaqueSessionId` from the cookie.
3. BFF looks up `tenant:{tid}:session:{opaqueId}` in Redis to hydrate claims.
4. If the key is missing (expired or revoked), the request is rejected with `401 Unauthorized`.
5. Claims (`sub`, `email`, `tid`, `roles[]`) are forwarded in an internal header to the Host.

---

*Source file: [`auth-flow.mmd`](auth-flow.mmd) | PNG: [`auth-flow.png`](auth-flow.png)*
*Last updated: 2026-05-16 | Next update trigger: external IdP integration (Phase 6+) or session revocation flow added*
