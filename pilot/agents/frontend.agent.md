# frontend.agent.md
# STRIDE — Frontend Agent

## Purpose
Persistent AI memory for Angular frontend implementation.
Read before implementing any Angular feature, component, or service.

---

## Technology Stack

- Angular (latest stable) — **standalone components architecture** (no NgModules)
- Tailwind CSS (utility-first styling)
- PrimeNG (enterprise UI component library)
- Angular Signals (reactive state — local and feature-level)
- RxJS (HTTP streams, async event-based operations)

---

## Standalone Components Architecture

This project uses Angular's modern standalone component model:
- No `NgModule` declarations anywhere
- Every component, directive, and pipe is `standalone: true`
- Feature routes are configured via `Routes` arrays exported from `{feature}.routes.ts`
- Lazy loading uses `loadComponent` (for single routes) or `loadChildren` pointing to a routes file
- `bootstrapApplication` in `main.ts` with `provideRouter`, `provideHttpClient`, etc.
- Shared utilities are imported directly into components that need them — no shared module

```typescript
// main.ts
bootstrapApplication(AppComponent, {
  providers: [
    provideRouter(appRoutes),
    provideHttpClient(withInterceptors([correlationInterceptor, errorInterceptor])),
    provideAnimations(),
  ]
});
```

---

## BFF Interaction Rules

The Angular SPA communicates ONLY with STRIDE.BFF.

Rules:
- All HTTP calls go to the BFF — never directly to backend modules
- No Bearer tokens in Angular code
- No JWT parsing in Angular
- No `localStorage` or `sessionStorage` for auth state
- Session managed via HttpOnly cookie (transparent to Angular)
- Angular checks auth state by calling a BFF `/auth/me` endpoint

---

## Folder Structure

```
frontend/
└── src/
    ├── app/
    │   ├── core/                  # Singleton services, interceptors, guards
    │   │   ├── auth/
    │   │   ├── tenant/
    │   │   ├── http/              # HTTP interceptors
    │   │   └── guards/
    │   ├── shared/                # Shared components, pipes, directives
    │   │   ├── components/
    │   │   ├── directives/
    │   │   └── pipes/
    │   ├── layout/                # Shell, nav, sidebar components
    │   │   ├── shell/
    │   │   ├── sidebar/
    │   │   └── topbar/
    │   └── features/              # Feature modules (lazy-loaded)
    │       ├── auth/              # Login, logout
    │       ├── dashboard/
    │       ├── workflows/
    │       ├── scheduling/
    │       ├── reporting/
    │       ├── notifications/
    │       └── administration/
    ├── environments/
    └── assets/
```

---

## Feature Module Structure

Each feature follows this pattern:

```
features/workflows/
├── components/           # Presentational components
├── pages/                # Routed page components (smart components)
├── services/             # Feature-scoped services (HTTP calls to BFF)
├── models/               # TypeScript interfaces/types
├── store/                # Signals-based state
└── workflows.routes.ts   # Lazy-loaded route config
```

---

## Routing Architecture

- All routes lazy-loaded via `loadChildren` or `loadComponent`
- Auth guard applied globally at shell level
- Role/permission guards applied per feature route
- Tenant-aware route guards must validate tenant context on init

---

## State Management

Use Angular Signals for:
- Local component state
- Feature-level reactive state
- Auth/session state in core

Use RxJS for:
- HTTP streams
- Event-based async operations
- WebSocket or push notification streams (future)

Do NOT introduce NgRx unless complexity justifiably demands it.

---

## Auth Guards

```typescript
// core/guards/auth.guard.ts
// Calls BFF /auth/me — if 401, redirect to /login

// core/guards/role.guard.ts
// Checks role claims from auth state signal
// If unauthorized, redirect to /forbidden

// core/guards/tenant.guard.ts
// Validates tenant context is initialized before entering features
```

---

## HTTP Interceptors

```typescript
// core/http/correlation.interceptor.ts
// Adds X-Correlation-Id header to every outbound request

// core/http/error.interceptor.ts
// Handles 401 (redirect to login), 403 (forbidden page), 500 (error notification)
```

---

## PrimeNG Usage

- Use PrimeNG components for: tables, dialogs, forms, dropdowns, toasts, charts
- Use Tailwind for layout, spacing, custom utility classes
- Do NOT override PrimeNG internals with deep CSS selectors
- Theme: configure once globally via PrimeNG theme configuration

---

## Component Standards

- Use OnPush change detection strategy for all components
- Use Signals for reactive state — avoid BehaviorSubject where Signals suffice
- Smart/dumb component separation: pages are smart, components are dumb
- Keep templates clean — extract complex logic into services or computed signals

---

## AI Constraints

MUST:
- Keep all HTTP calls proxied through BFF
- Use Signals for state where appropriate
- Lazy-load all feature modules
- Apply auth guard at root level

MUST NOT:
- Store auth tokens in browser storage
- Call backend modules directly
- Import feature modules eagerly
- Use `any` types — prefer typed interfaces
- Use `ngModel` with two-way binding on complex forms — use reactive forms
