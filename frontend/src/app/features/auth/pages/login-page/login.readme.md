# Login Flow — Knowledge Transfer

> **Component:** `LoginPageComponent`  
> **Path:** `frontend/src/app/features/auth/pages/login-page/`  
> **Story:** US-030 (EP-015 — Angular Login UI)  
> **Related ADRs:** ADR-007, ADR-008  

---

## Overview

The login page is the entry-point for all authenticated sessions. It handles credential collection, BFF round-trip authentication, and post-login navigation — while never touching a JWT token directly.

---

## Architecture: BFF Cookie Pattern (ADR-007, ADR-008)

```
User → Angular Login Page
         ↓  POST /bff/auth/login  { email, password }
       STRIDE.BFF (ASP.NET Core)
         ↓  forwards credentials
       STRIDE.Host  /api/identity/auth/login
         ↓  returns JWT
       BFF validates JWT, writes HttpOnly session cookie
         ↓  GET /bff/auth/me  (checkSession)
       BFF reads session cookie → returns AuthUser JSON
         ↓
       AuthService._user signal populated
         ↓
       Component navigates → /dashboard
```

**Key rule:** Angular **never** holds or reads a JWT token. The BFF issues a `HttpOnly` cookie (`stride.session`) that the browser sends automatically. Angular only receives `AuthUser` (userId, email, tenantId, roles) from `/bff/auth/me`.

---

## Files in This Component

| File | Purpose |
|---|---|
| `login-page.ts` | Component class — form setup, signal state, submit logic |
| `login-page.html` | Template — deep indigo gradient card, reactive form, error states |
| `login-page.scss` | Component styles — all sourced from `--stride-*` CSS tokens |
| `login.readme.md` | This document |

---

## Component Structure

### Signals

| Signal | Type | Description |
|---|---|---|
| `isLoading` | `Signal<boolean>` | True while the BFF round-trip is in-flight. Disables submit button and shows spinner. |
| `errorMessage` | `Signal<string \| null>` | Server or network error displayed in the alert banner. Cleared on each new submit attempt. |
| `showPassword` | `Signal<boolean>` | Toggles password input type between `password` and `text`. |

### Form Controls

| Control | Validators | Notes |
|---|---|---|
| `email` | `required`, `email` | Autocomplete `email` |
| `password` | `required`, `minLength(8)` | Autocomplete `current-password` |
| `rememberMe` | none | Checkbox. Currently captured but not forwarded to BFF (Phase 6: implement persistent session TTL). |

### Submit Flow

```
onSubmit()
  ├── loginForm.invalid? → markAllAsTouched(), return (client-side validation)
  ├── isLoading.set(true), errorMessage.set(null)
  ├── authService.login(email, password)
  │     ├── POST /bff/auth/login
  │     └── switchMap → GET /bff/auth/me  (sets AuthService._user signal)
  ├── finalize() → isLoading.set(false)  [always runs]
  ├── next()  → router.navigate(['/dashboard'])
  └── error() → errorMessage.set(parsed message)
```

---

## AuthService Contract

```typescript
// Returns Observable<AuthUser> — the user signal is populated BEFORE next() fires.
login(email: string, password: string): Observable<AuthUser>

// Returns Observable<AuthUser> — used by authGuard and APP_INITIALIZER (US-031).
checkSession(): Observable<AuthUser>
```

**Why `switchMap` matters:** The original stub used `tap(() => this.checkSession().subscribe())` which fire-and-forgot `checkSession`. This meant the component's `next` callback fired with `_user` still `null`. Replaced with `switchMap(() => this.checkSession())` so the outer observable only completes after the user signal is set.

---

## Error Handling

The BFF returns `ProblemDetails` on failures:

```json
{ "title": "Unauthorized", "status": 401, "message": "Invalid credentials" }
```

The component reads `err.error.message ?? err.error.title ?? 'Invalid email or password…'` and surfaces it in the `auth-alert--error` banner. The `errorInterceptor` (`core/http/error.interceptor.ts`) redirects 401s to `/login` globally — but the login page itself suppresses that redirect because it handles 401 inline.

> ⚠️ **Note:** The `errorInterceptor` currently navigates to `/login` on every 401. This causes a navigation loop if the BFF returns 401 on a bad login attempt. **US-031 / US-030 follow-up:** Add a route check in the interceptor — skip navigation if `router.url === '/login'`.

---

## Navigation

| Condition | Target |
|---|---|
| Successful login | `/dashboard` |
| Already authenticated (authGuard) | `/dashboard` (guard redirects automatically) |
| Session expired / 401 elsewhere | `/login` (errorInterceptor) |
| Not authenticated, guard blocks route | `/login` (authGuard catchError) |

---

## Design System

Styles use `--stride-*` CSS custom properties from `frontend/src/styles.scss`. Key tokens:

| Token | Value (light) | Usage |
|---|---|---|
| `--stride-primary` | `#6366F1` | Input focus ring, submit button, links |
| `--stride-primary-hover` | `#4F46E5` | Button hover state |
| `--stride-primary-ring` | `rgba(99,102,241,0.22)` | Focus box-shadow |
| `--stride-error` | `#EF4444` | Field error border, alert text |
| `--stride-error-bg` | `#FEF2F2` | Alert background |

The background is a CSS `linear-gradient` animation — **not** a Tailwind class — because the `background-size: 300% 300%` drift animation requires values outside Tailwind's default scale.

---

## Accessibility

- All inputs have `id` + `<label for="…">` pairs
- Error messages use `role="alert"` + `aria-live="assertive"` for screen reader announcements
- Inputs carry `aria-invalid="true"` when touched and invalid
- Password toggle has `aria-label` and `aria-pressed` reflecting visibility state
- Submit button carries `aria-busy` while loading
- All interactive elements have visible `:focus-visible` outlines

---

## Future Work

| Item | Phase | Notes |
|---|---|---|
| `rememberMe` → BFF session TTL | Phase 6 | Pass `persistent: true` in request body; BFF sets longer cookie max-age |
| SSO (Azure AD / Auth0) | Phase 6 | `/bff/auth/sso` redirect; SSO button on login page is currently disabled |
| MFA / TOTP screen | Phase 6 | New `/mfa` route; BFF returns `mfa_required` challenge |
| `errorInterceptor` loop fix | US-031 | Check `router.url !== '/login'` before navigating on 401 |
| Unit tests | Any | `LoginPageComponent` + `AuthService.login()` switchMap chain |

---

## Testing Notes

```typescript
// Minimal unit test scaffold
describe('LoginPageComponent', () => {
  let fixture: ComponentFixture<LoginPageComponent>;
  let authService: jasmine.SpyObj<AuthService>;
  let router: Router;

  beforeEach(() => {
    authService = jasmine.createSpyObj('AuthService', ['login']);
    TestBed.configureTestingModule({
      imports: [LoginPageComponent],
      providers: [
        { provide: AuthService, useValue: authService },
        provideRouter([]),
      ],
    });
    fixture = TestBed.createComponent(LoginPageComponent);
  });

  it('should mark all touched and not call login if form is invalid', () => {
    fixture.componentInstance.onSubmit();
    expect(authService.login).not.toHaveBeenCalled();
  });

  it('should call authService.login with form values', () => {
    authService.login.and.returnValue(of({ userId: '1', email: 'a@b.com', tenantId: 't1', roles: [] }));
    fixture.componentInstance.loginForm.setValue({ email: 'a@b.com', password: 'password123', rememberMe: false });
    fixture.componentInstance.onSubmit();
    expect(authService.login).toHaveBeenCalledWith('a@b.com', 'password123');
  });
});
```

---

## Related Files

| File | Role |
|---|---|
| `core/auth/auth.service.ts` | `login()`, `checkSession()`, `_user` signal |
| `core/guards/auth.guard.ts` | Protects all routes except `/login`; calls `checkSession` |
| `core/http/error.interceptor.ts` | Global 401 → `/login` redirect |
| `app.routes.ts` | Route definition for `/login` (no guard, lazy-loaded) |
| `pilot/docs/ui-mockups/02-login.html` | UI prototype — source of truth for visual design |
