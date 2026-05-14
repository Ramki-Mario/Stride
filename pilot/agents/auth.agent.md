# auth.agent.md
# STRIDE — Authentication & Authorization Agent

## Purpose
Persistent AI memory for auth implementation.
Read before implementing any auth, session, BFF, or tenant resolution code.

---

## Auth Architecture Summary

```
Browser (Angular SPA)
    ↓  (credentials only — never receives tokens)
STRIDE.BFF
    ↓  (forwards auth request)
STRIDE.Modules.Identity
    ↓  (validates credentials, resolves tenant, builds claims)
Redis (session store)
    ↓  (session stored)
STRIDE.BFF  (issues HttpOnly cookie)
    ↓
Browser (receives cookie only)
```

---

## STRIDE.BFF Responsibilities

- Receives login request from Angular
- Forwards to Identity module
- Receives session/token from Identity
- Stores session in Redis
- Issues HttpOnly + Secure + SameSite=Strict cookie to browser
- Handles logout (invalidates Redis session)
- Handles token refresh transparently
- Aggregates requests for frontend (proxy to internal APIs)
- Does NOT contain business logic

---

## What the Frontend MUST NOT Do

- Store access tokens in `localStorage` or `sessionStorage`
- Manage JWT lifecycle
- Call backend APIs directly with Bearer tokens
- Handle refresh token rotation
- Decode JWTs

---

## Tenant Resolution Flow

```
Login Request arrives at BFF
    ↓
Extract email domain
    ↓
Is domain corporate? (not gmail/outlook/yahoo/hotmail/etc.)
    ├── YES → TenantDomainMapping lookup → TenantId
    └── NO  → UserTenantMapping lookup → TenantId
    ↓
TenantId resolved
    ↓
Identity validation with TenantId context
    ↓
Session creation
```

---

## Session Architecture

Session stored in Redis:
```
Key:   tenant:{tenantId}:session:{sessionId}
Value: {
  userId, tenantId, roles, permissions, email,
  correlationId, createdAt, expiresAt, refreshToken
}
```

Session TTL: configurable (default: 8 hours)
Refresh window: configurable (default: 30 minutes before expiry)

---

## Claims Structure

```csharp
// Claims issued per session
ClaimTypes.NameIdentifier → UserId
"tid"                      → TenantId
ClaimTypes.Role            → Role (one or more)
"permissions"              → comma-separated permission list
"module_access"            → comma-separated accessible modules
"email"                    → user email
"session_id"               → Redis session key reference
```

---

## RBAC Roles

```
Admin
OperationsManager
FinanceUser
FieldWorker
Supervisor
```

Roles map to permission sets.
Claims-based authorization used for fine-grained access control.

---

## Multi-Tenant Auth Rules

- Every request must carry a validated TenantId claim
- Sessions are tenant-bound — cannot be reused across tenants
- Users cannot access cross-tenant resources even with valid session
- Tenant validation happens in `TenantMiddleware` on every request
- If TenantId is missing or invalid → 401 Unauthorized

---

## Auth Events — Must Be Logged

Every event must log: TenantId, UserId, CorrelationId, RequestId, IP address, SessionId, timestamp.

Events:
- `auth.login.success`
- `auth.login.failed`
- `auth.logout`
- `auth.token.refreshed`
- `auth.session.expired`
- `auth.rbac.denied`
- `auth.tenant.mismatch`
- `auth.session.invalidated`

---

## Cookie Configuration

```csharp
options.Cookie.HttpOnly = true;
options.Cookie.Secure   = true;
options.Cookie.SameSite = SameSiteMode.Strict;
options.Cookie.Name     = "__stride_session";
```

---

## Future IdP Readiness

The Identity module must be designed with an abstraction layer so that:
- Internal auth can be replaced by Azure AD, Auth0, or Okta
- The BFF layer does NOT change when switching IdP
- Session/cookie strategy remains unchanged
- RBAC and claims remain consistent regardless of IdP

See: auth-future-update.md

---

## AI Constraints

MUST:
- Preserve HttpOnly cookie strategy on all auth paths
- Preserve TenantId in every session and claim
- Preserve Redis as the session store
- Preserve BFF as the only auth surface for the frontend
- Log all auth events with full context

MUST NOT:
- Add localStorage token storage
- Bypass tenant middleware
- Expose JWTs or refresh tokens to Angular
- Tightly couple to a specific IdP
- Allow cross-tenant sessions
