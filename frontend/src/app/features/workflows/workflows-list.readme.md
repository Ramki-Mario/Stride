# Workflow List Page — KT Document

> **Story:** US-054 · EP-022  
> **Route:** `/workflows` (lazy-loaded via `workflowRoutes`)  
> **Design ref:** `pilot/docs/ui-mockups/04-workflow-list.html`

---

## File Structure

```
features/workflows/
├── models/
│   └── workflow.models.ts            WorkflowDefinitionSummary, WorkflowStatus, STATUS_CONFIG
├── services/
│   └── workflow.service.ts           HTTP calls to /bff/workflows/...
├── pages/
│   └── workflows-page/
│       ├── workflows-page.ts         Component class (Signals + OnPush)
│       ├── workflows-page.html       Template (Angular control-flow syntax)
│       └── workflows-page.scss       Component-scoped styles (STRIDE tokens)
├── workflows.routes.ts               Lazy routes for this feature
└── workflows-list.readme.md          This file
```

---

## Component Architecture

**Change detection:** `OnPush` — all state is held in Signals; Angular only re-renders when a signal changes.

### Signals (state)

| Signal | Type | Description |
|--------|------|-------------|
| `workflows` | `WorkflowDefinitionSummary[]` | Raw data from BFF |
| `isLoading` | `boolean` | True during HTTP fetch |
| `errorMessage` | `string \| null` | Shown in error banner |
| `searchTerm` | `string` | Live search input |
| `statusFilter` | `WorkflowStatus \| ''` | Active status filter |
| `sortBy` | `SortKey` | Current sort column |
| `viewMode` | `'table' \| 'cards'` | Table or card view |
| `currentPage` | `number` | 1-based current page |
| `selectedIds` | `Set<string>` | IDs of checked rows |

### Computed signals

| Computed | Derives from | Description |
|----------|-------------|-------------|
| `filteredWorkflows` | `workflows`, `searchTerm`, `statusFilter`, `sortBy` | Filtered + sorted list |
| `paginatedWorkflows` | `filteredWorkflows`, `currentPage` | Current page slice (10 items) |
| `totalPages` | `filteredWorkflows` | Ceiling of count / PAGE_SIZE |
| `statusCounts` | `workflows` | `{ all, draft, active, archived }` |
| `selectionCount` | `selectedIds` | Number of selected rows |
| `pagesArray` | `totalPages` | `[1, 2, 3, …]` for pagination UI |
| `showingFrom/To` | `currentPage`, `filteredWorkflows` | "Showing 1–10 of 48" |

### Auto-reset effect

```typescript
effect(() => {
  this.searchTerm(); this.statusFilter(); this.sortBy();
  this.currentPage.set(1);   // reset page whenever any filter changes
});
```

---

## Template Features

| Feature | Implementation |
|---------|---------------|
| Summary strip | 4 clickable status-count cards; active card highlighted via `[class.active]` |
| Search | `[(ngModel)]`-bound; filters `name`, `description`, `id` |
| Status filter | `<select>` bound to `statusFilter` signal |
| Sort | `<select>` bound to `sortBy` signal; column headers also clickable |
| Table view | Native `<table>` with `@for` row loop, `[class.selected]`, hover row actions |
| Card view | CSS grid, `@for` card loop, same data as table |
| View toggle | Two icon buttons toggling `viewMode()` |
| Bulk select | Master checkbox in `<thead>` + per-row checkboxes; bulk delete button |
| Status badges | `.wfl-badge` + `[ngClass]="statusClass(wf.status)"` → `.badge-active`, `.badge-draft`, etc. |
| Pagination | Previous/next + page number buttons; hidden when all items fit on one page |
| Loading skeleton | 5-row animated shimmer table shown while `isLoading()` is true |
| Empty state | Two variants: no data at all vs. no results matching current filters |
| Error banner | Shown on HTTP failure; includes Retry button |
| Keyboard nav | All interactive elements keyboard-accessible; `tabindex`, `aria-*` attributes |

---

## Data Flow

```
WorkflowsPageComponent.ngOnInit()
  → WorkflowService.getDefinitions()
      → GET /bff/workflows/definitions
          (BFF proxies to GET /api/workflows on STRIDE.Host)
              → ListWorkflowDefinitionsQuery (MediatR)
                  → WorkflowDefinitionRepository.GetAllAsync()
                      → SQL: SELECT * FROM workflows.WorkflowDefinitions WHERE TenantId=@tid AND IsDeleted=0
  ← WorkflowDefinitionSummary[]
  → workflows.set(data)
  → filteredWorkflows / paginatedWorkflows recomputed automatically
```

---

## BFF Note

`/bff/workflows/definitions` is the **intended** BFF endpoint. The BFF workflow proxy is not yet wired (deferred epic). Until BFF forwarding rules are added, this endpoint will return 404 in local dev.

**To enable for local testing:** add a proxy config entry in `proxy.conf.json`:
```json
"/bff/workflows": {
  "target": "https://localhost:7001",
  "secure": false,
  "changeOrigin": true,
  "pathRewrite": { "^/bff/workflows": "/api/workflows" }
}
```

---

## What Comes Next

| Story | What it adds |
|-------|-------------|
| US-055 | Workflow Detail page (single definition view + step list) |
| US-056 | Create/Edit Workflow form (modal or page) |
| US-057 | Step detail / action modals (assign, complete, fail, skip) |
