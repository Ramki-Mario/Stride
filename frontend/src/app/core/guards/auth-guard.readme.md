# Auth Guard & Session Bootstrap — Knowledge Transfer

> **Files:** `auth.guard.ts`, `app.config.ts` (APP_INITIALIZER), `core/http/error.interceptor.ts`  
> **Story:** US-031 (EP-015 — Angular Login UI)  
> **Related ADRs:** ADR-007, ADR-008  

---

## Overview

Three pieces work together to guard protected routes and restore sessions on hard refresh:

```
1. APP_INITIALIZER   — runs once on boot, before any route activates
2. authGuard         — runs on every protected route activation
3. errorInterceptor  — handles 401/403 from any HTTP call
```

---

## 1. APP_INITIALIZER — `app.config.ts`

```typescript
function initSession() {
  const auth = inject(AuthService);
  return () => firstValueFrom(auth.checkSession(), { defaultValue: null });
}

{ provide: APP_INITIALIZER, useFactory: initSession, multi: true }
```

### What it does
Calls `GET /bff/auth/me` before Angular renders anything. If the browser has a valid `stride.session` HttpOnly cookie, the BFF responds with `AuthUser` JSON and `AuthService._user` signal is populated.

### Why `firstValueFrom` with `{ defaultValue: null }`
`APP_INITIALIZER` factories must return a Promise that resolves (never rejects) or Angular hangs. Using `{ defaultValue: null }` means a 401 (unauthenticated) is treated as a resolved `null` — not an error. The `_user` signal stays `null`; `authGuard` handles the redirect.

### Boot sequence

```
App boot
  └─ APP_INITIALIZER fires
       └─ GET /bff/auth/me
            ├─ 200 → _user signal set → isAuthenticated() = true
            └─ 401 → _user stays null → isAuthenticated() = false
  └─ Router activates first route
       └─ authGuard runs (see §2)
```

### Why this matters (the flicker problem)
Without `APP_INITIALIZER`, even a logged-in user gets momentarily redirected to `/login` on hard refresh because `_user` is `null` when the router first activates. With `APP_INITIALIZER`, the signal is populated before any routing begins — no redirect, no flicker.

---

## 2. authGuard — `auth.guard.ts`

```typescript
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isAuthenticated()) return true;  // ← fast path after APP_INITIALIZER

  return auth.checkSession().pipe(           // ← safety net only
    map(() => true),
    catchError(() => of(router.createUrlTree(['/login'])))
  );
};
```

### Flow after APP_INITIALIZER ran

| `isAuthenticated()` | What happens |
|---|---|
| `true` (session valid) | Returns `true` immediately — no HTTP call |
| `false` (not logged in) | Calls `checkSession()` as safety net → 401 → `catchError` → redirects to `/login` |

### When does the `checkSession()` safety net fire?
- User was not authenticated at boot (expected path)
- Session expired mid-session and user navigates to a different protected route
- Edge case: router activated before `APP_INITIALIZER` completed (shouldn't happen, but defensive)

### Routes protected by this guard
Defined in `app.routes.ts`:
```typescript
{ path: '', canActivate: [authGuard], component: ShellComponent, children: [...] }
```
All feature routes (dashboard, workflows, scheduling, reporting, notifications, administration) are children of the shell and therefore protected.

---

## 3. errorInterceptor — `core/http/error.interceptor.ts`

```typescript
if (error.status === 401) {
  const isAuthFlowEndpoint = /\/bff\/auth\/(login|me)$/.test(req.url);
  if (!isAuthFlowEndpoint) router.navigate(['/login']);
}
if (error.status === 403) router.navigate(['/forbidden']);
```

### Why auth flow endpoints are suppressed

| Endpoint | Returns 401 when | Who handles it |
|---|---|---|
| `/bff/auth/login` | Wrong credentials | `LoginPageComponent.errorMessage` signal |
| `/bff/auth/me` | Not logged in | `APP_INITIALIZER` resolves null; `authGuard` redirects |
| All others | Session expired | `errorInterceptor` redirects to `/login` |

Without this suppression, a wrong password on the login form would trigger both:
1. `LoginPageComponent`'s `error()` callback (shows inline error) ← correct
2. `errorInterceptor`'s `router.navigate(['/login'])` ← redundant and causes navigation loop

---

## Session Lifecycle

```
Hard refresh (logged in)
  APP_INITIALIZER → GET /bff/auth/me → 200 → _user set
  authGuard → isAuthenticated() = true → allowed ✓

Hard refresh (not logged in)
  APP_INITIALIZER → GET /bff/auth/me → 401 → defaultValue null
  authGuard → isAuthenticated() = false → checkSession() → 401 → /login ✓

Login
  LoginPageComponent.onSubmit() → POST /bff/auth/login → 200
    → switchMap → GET /bff/auth/me → 200 → _user set
    → navigate(['/dashboard']) ✓

Wrong password
  LoginPageComponent.onSubmit() → POST /bff/auth/login → 401
    → error interceptor suppresses (isAuthFlowEndpoint = true)
    → component error() callback → errorMessage signal set ✓

Session expires mid-session
  Any API call → 401 (not auth flow endpoint)
    → errorInterceptor → navigate(['/login']) ✓

Logout
  AuthService.logout() → POST /bff/auth/logout
    → BFF clears Redis session + expires cookie
    → _user.set(null) + navigate(['/login']) ✓
```

---

## Testing Notes

```typescript
// APP_INITIALIZER integration test sketch
it('should populate user signal on boot if session is valid', async () => {
  const auth = TestBed.inject(AuthService);
  const http = TestBed.inject(HttpTestingController);

  // APP_INITIALIZER fires during TestBed.inject
  const req = http.expectOne('/bff/auth/me');
  req.flush({ userId: '1', email: 'a@b.com', tenantId: 't1', roles: ['Admin'] });

  expect(auth.isAuthenticated()).toBe(true);
});

it('should resolve gracefully on 401', async () => {
  const auth = TestBed.inject(AuthService);
  const http = TestBed.inject(HttpTestingController);

  const req = http.expectOne('/bff/auth/me');
  req.flush('', { status: 401, statusText: 'Unauthorized' });

  expect(auth.isAuthenticated()).toBe(false);
});
```

---

## Related Files

| File | Role |
|---|---|
| `core/auth/auth.service.ts` | `checkSession()`, `_user` signal, `isAuthenticated` computed |
| `app.routes.ts` | Route definitions — `authGuard` applied to shell route |
| `features/auth/pages/login-page/login.readme.md` | Login component KT (US-030) |
| `core/http/error.interceptor.ts` | Global 401/403 handler with auth-flow suppression |
