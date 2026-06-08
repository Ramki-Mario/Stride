# STRIDE Shared Memory — AI Collaboration Handoff Document

> **Who this is for:** Codex (or any AI collaborator picking up STRIDE work while Claude is unavailable).
> **Written by:** Claude (Sonnet 4.6) — 2026-06-08
> **Why it exists:** Claude operates with a 5-hour session token limit with a 3-hour reset window. During the gap, Codex continues the work. This document is the complete briefing so Codex can work without losing context, and so Claude knows exactly what happened when it comes back.

---

## 1. What Is STRIDE?

**STRIDE = Smart Task Routing, Integration & Distributed Execution.**

It is a **multi-tenant operational orchestration platform** — an enterprise-grade SaaS product being built as a portfolio project targeting international contract opportunities. The long-term goal is a live production SaaS product for workforce/operational management.

### What Problem Does It Solve?

Organizations (construction firms, field-service companies, staffing agencies, etc.) need to:
- Define repeatable **workflow templates** (e.g., "Employee Onboarding", "Site Inspection")
- **Launch instances** of those workflows when a job starts
- Route each step to the right person based on their **role**
- **Capture structured data** at each step (hours worked, materials used, sign-offs)
- Track billable work, generate invoices, and attach files/photos
- Get deadline alerts, notifications, and reporting analytics

STRIDE is that platform — multi-tenant (each company is a tenant), clean architecture, enterprise patterns, portfolio-grade code quality.

### Where Is It Being Built?

- **Repository:** https://github.com/Ramki-Mario/Stride (branch: `develop`)
- **Local path:** `D:/Repos/stride`
- **Owner:** Ramki (the human developer, raam2398@gmail.com)
- **AI pair programmers:** Claude (primary) + Codex (backup/parallel)

---

## 2. Architecture Overview

### System Layers

```
Angular SPA (frontend/) → BFF (STRIDE.BFF) → Host API (STRIDE.Host)
                                                    ↕
                               Modules (Identity | Workflows | Clients | Invoicing | 
                                        Notifications | Reporting | Scheduling |
                                        Administration)
                                    ↕
                               SQL Server (local: LAPTOP-417EMKN1\SQLEXPRESS)
                               Redis Cloud (sessions + cache)
```

### Backend Module Pattern (Clean Architecture)

Each module has **4 source layers** and **2 test layers**:
```
STRIDE.Modules.{Module}.Domain          — Entities, Value Objects, Enums, Domain Events, Exceptions
STRIDE.Modules.{Module}.Application     — Commands, Queries, Handlers (MediatR CQRS), DTOs, Abstractions
STRIDE.Modules.{Module}.Infrastructure  — EF Core DbContext + Configs + Migrations + Dapper read services
STRIDE.Modules.{Module}.API             — Controllers + Request DTOs

tests/
  STRIDE.Modules.{Module}.Domain.Tests
  STRIDE.Modules.{Module}.Application.Tests
```

**Dependency rule:** Domain ← Application ← Infrastructure ← API. Cross-module calls go through Application abstractions only (no direct Infrastructure to Infrastructure).

### Frontend Pattern

- **Angular 21** with standalone components, `ChangeDetectionStrategy.OnPush`
- **Signals** (`signal()`, `computed()`, `toSignal()`) for state — NOT `ngOnChanges`
- **`@for`, `@if`, `@else`** control flow — NOT `*ngFor`, `*ngIf`
- **BFF-only** — Angular NEVER calls the Host API directly. All calls go to `/bff/*`
- Feature folders: `features/workflows/`, `features/clients/`, `features/invoicing/`, etc.
- File naming: `workflow-form-page.ts` (no `.component.` infix)

### Key Infrastructure

| Item | Value |
|---|---|
| SQL Server | `LAPTOP-417EMKN1\SQLEXPRESS`, DB = `STRIDE`, Windows Auth |
| Redis | Redis Cloud: `redis-18113.crce300.ap-south-1-2.ec2.cloud.redislabs.com:18113` |
| Backend port | `http://localhost:5001` (Host) |
| BFF port | `http://localhost:5000` |
| Frontend | `http://localhost:4200` |
| dotnet-ef | Globally installed v10.0.8 |

**Connection string (Host appsettings.Development.json — NEVER commit passwords):**
```
Server=LAPTOP-417EMKN1\SQLEXPRESS;Database=STRIDE;Integrated Security=true;TrustServerCertificate=true;
```

### EF Migration Command

Always use the Infrastructure project as startup-project (Host doesn't have EF Design tools):
```powershell
dotnet ef migrations add MigrationName `
  --project backend/src/Modules/{Module}/STRIDE.Modules.{Module}.Infrastructure `
  --startup-project backend/src/Modules/{Module}/STRIDE.Modules.{Module}.Infrastructure
```

---

## 3. Current Project State (as of 2026-06-08)

### Phase Progress

| Phase | Status |
|---|---|
| Phase 1 — Monorepo & Foundation | ✅ Complete |
| Phase 2 — Identity & Tenant | ✅ Complete |
| Phase 3 — Core Workflow Engine | ✅ Complete |
| Phase 4 — Dashboard & Reporting | ✅ Complete |
| Phase 5 — Notifications & Observability | ✅ Complete |
| Phase 6 — SaaS Readiness | ✅ Complete |
| **Phase 7 — Portfolio & Deployment Polish** | **🔵 In Progress** |

### Phase 7 — Milestone #11 "Product Layer Gaps"

14 EPICs (EP-048 to EP-061), covering 40 user stories (US-147 to US-186) plus 3 sub-stories = 43 issues (#263–316).

**Completed EPICs:**

| Epic | Description | Status | Key Commits |
|---|---|---|---|
| EP-048 | Step Ownership & Assignment Model (US-147–149) | ✅ Done | `b0f73c5`, `1f857ac`, `4948895` |
| EP-049 | Client/Customer Entity (US-150–152) | ✅ Done | `381fc14`, `b82d9d0`, `56211b3` |
| EP-050 | Workflow ↔ Invoice Integration (US-153–155) | ✅ Done | `fcf4728`, `84c3533`, `b1b3e45` |
| EP-051 | File & Photo Attachments (US-156–158) | ✅ Done | `84e80fa`, `dcbd637`, `b707f3e` |

**In Progress:**

| Epic | Description | Stories | Next |
|---|---|---|---|
| **EP-052** | Advanced Step Types (#267) | US-159 ✅ `3d2cefb`, US-160 🔲, US-161 🔲 | **US-160 is next** |

**Remaining EPICs (all in Backlog):**

EP-053 SLA & Deadline Tracking → EP-054 File Attachments → EP-055 Comments & Activity Log →
EP-056 Team/Department Entity → EP-057 Custom Fields → EP-058 Reporting Enhancements →
EP-059 Public API/Webhooks → EP-060 Mobile-Responsive Shell → EP-061 Accessibility & i18n

### What Just Landed (US-159) — `3d2cefb`

The step field schema definition feature. Summary of what's in the codebase:

**Domain:**
- `StepFieldType` enum: `Text | Number | Currency | Hours | Dropdown | Date | Boolean | LongText`
- `StepFieldDefinition` entity with `Label`, `FieldType`, `IsRequired`, `DisplayOrder`, `HelpText`, `DropdownOptionsJson` (serialized), computed `DropdownOptions`
- `StepDefinition` now has `Fields` collection with `AddField()`, `RemoveField()`, `MoveFieldUp()`, `MoveFieldDown()`
- `WorkflowDefinition.AddStep()` accepts optional `fields` tuple list to seed fields at creation time
- `InternalsVisibleTo` for Domain.Tests added to Domain.csproj

**Infrastructure:**
- `StepFieldDefinitionConfiguration` — table `workflows.StepFieldDefinitions`, FK to `StepDefinitions` (Cascade)
- EF migration `20260608064809_AddStepFieldDefinitions` applied
- `WorkflowDefinitionRepository` eager-loads `.ThenInclude(s => s.Fields)`

**Application:**
- `FieldDefinitionRequest` record on `CreateWorkflowCommand`
- `FieldDefinitionDto` + `StepDefinitionDto.Fields` in `WorkflowDefinitionDto`
- Both `CreateWorkflowCommandHandler` and `GetWorkflowDefinitionQueryHandler` updated

**API:**
- `StepRequestDto` now includes `RequiredRoleId` (was missing — bug fixed) and `FieldDefinitions`
- `WorkflowsController.CreateDefinition` maps both

**Angular:**
- `FieldType` union type + `FIELD_TYPE_LABELS` + `FIELD_TYPES` in `workflow.models.ts`
- `FieldDefinition` (read model) and `FieldDefinitionRequest` (write model) interfaces
- `workflow-form-page` has full field editor: add/remove/reorder fields per step, type select, required toggle, help text, dropdown options input
- Fixed Angular `@for` nested index collision: outer loop = `let si = $index`, inner field loop = `track $index; let fi = $index` (important: `si` cannot appear inside `track` expression of inner loop)

**Tests:** 11 domain + 5 application = 53 domain / 84 application total, all green

### What Is Next — US-160 (#290)

**US-160: Field capture at runtime (step completion)**

When a user completes a step, they should see a form rendered from the step's `Fields` definition. They fill in values. Those values are submitted with the `CompleteStep` command.

**Size: L**

Key work:
1. `StepFieldValue` domain entity (StepInstanceId FK, FieldDefinitionId FK, Value stored as string)
2. `CompleteStepCommand` extended with `IReadOnlyList<FieldValueInput>?`
3. EF migration `AddStepFieldValues`
4. `StepFieldValuesRepository` or extend existing infra
5. Angular `step-action-modal` gets a dynamic field form rendered from the step's field definitions
6. BFF + API mapping

**Then US-161 (#291): Field value storage/retrieval** — Read back stored field values on step instance detail, display them in the workflow detail page.

---

## 4. The 3-Layer Memory Architecture

This project uses a persistent 3-layer memory system. **You must update all 3 layers after every story completion.**

### Layer 1 — Pilot Folder (local files in repo)

Located at `D:/Repos/stride/pilot/`

| File | Purpose | When to Update |
|---|---|---|
| `pilot/current-status.md` | Phase progress, completed/pending stories | After every story |
| `pilot/sprint.md` | Detailed sprint log — what was built in each story | After every story |
| `pilot/agents/*.agent.md` | Per-domain agent guidance | When a new pattern is established |
| `pilot/discussions.md` | ADR log | When a new architectural decision is made |

### Layer 2 — GitHub (remote source of truth)

| Action | When |
|---|---|
| Close the GitHub issue for the story (`gh issue close {number}`) | After commit |
| Move board item to Done | After closing issue |
| Close the Epic issue if all stories in it are done | After last story in epic |

**Board IDs (memorize these):**
- Project node ID: `PVT_kwHOEHH9784BXrk6`
- Status field ID: `PVTSSF_lAHOEHH9784BXrk6zhS2zo4`
- Backlog option: `cf6478b5`
- In Progress option: `94f73086`
- In Review option: `1dd3696b`
- Done option: `e0bd69cc`

**Finding a board item ID (pagination required — 261 items, need 3 pages):**
```powershell
# Get all items across pages
$cursor1Result = gh api graphql -f query='
query($pid: ID!) { node(id: $pid) { ... on ProjectV2 { items(first: 100) { 
  pageInfo { endCursor hasNextPage }
  nodes { id content { ... on Issue { number } } }
}}}}' -f pid="PVT_kwHOEHH9784BXrk6" | ConvertFrom-Json

# Use cursor to get next page...
# Filter: $allNodes | Where-Object { $_.content.number -eq {issueNumber} }
```

**Move item to Done:**
```powershell
gh api graphql -f query='
mutation($project: ID!, $item: ID!, $field: ID!, $value: String!) {
  updateProjectV2ItemFieldValue(input: {
    projectId: $project itemId: $item fieldId: $field value: { singleSelectOptionId: $value }
  }) { projectV2Item { id } }
}' -f project="PVT_kwHOEHH9784BXrk6" -f item="{PVTI_...}" \
   -f field="PVTSSF_lAHOEHH9784BXrk6zhS2zo4" -f value="e0bd69cc"
```

### Layer 3 — Claude Memory (persistent AI memory)

Located at: `C:/Users/rmrra/.claude/projects/D--Repos-stride/memory/`

| File | Purpose |
|---|---|
| `MEMORY.md` | Single-line index with current status and key patterns |
| `project_stride_overview.md` | Full deep-dive reference document |

**After every story, update `MEMORY.md`** — update the inline status line to reflect:
- Which EP/US is now ✅ DONE
- Which US is next
- Any new patterns established

---

## 5. Mandatory After-Story Protocol (DO THIS EVERY TIME)

When you finish implementing and committing a story, always do **all** of the following:

### Step 1 — Commit
```powershell
# Stage specific files (never git add -A — might include secrets)
git add <specific files>

# Commit (NO Co-Authored-By line — ever)
git commit -m "feat(EP-0XX US-YYY): description of what was built"
```

### Step 2 — Close the GitHub issue
```powershell
gh issue close {issue_number} --comment "Implemented in {commit_hash} — {brief description}"
```

### Step 3 — Move board item to Done
```powershell
# 1. Find the item ID (see board query above)
# 2. Move to Done via GraphQL mutation
```

### Step 4 — If all stories in an epic are done, close the epic issue
```powershell
gh issue close {epic_issue_number} --comment "EP-0XX complete — all stories merged."
```

### Step 5 — Update Layer 3 memory (MEMORY.md)
Edit `C:/Users/rmrra/.claude/projects/D--Repos-stride/memory/MEMORY.md`:
- Mark the story as ✅ DONE with commit hash and issue number
- Add the commit hash to the EP-052 status line
- Update "next:" to point to the next story
- Add any new patterns to the patterns list

### Step 6 — Write the handoff entry in this file

**Add a new section at the bottom of this file** (see Section 9 below) with:
```markdown
## Codex Session — {date}

**Stories completed:** US-XXX (#NNN) `{commit hash}`
**What was built:** {brief description}
**New patterns established:** {if any}
**Tests added:** {count} — {pass/fail}
**Next story:** US-XXX (#NNN) — {description}
**Any issues encountered:** {if any}
**Memory updated:** MEMORY.md ✅ | GitHub issue closed ✅ | Board moved to Done ✅
```

This is the **most important step** — it's how Claude knows exactly what happened while it was away.

---

## 6. Critical Rules — DO NOT VIOLATE

These are non-negotiable. Violating them will break things or create security issues:

### Security
- ❌ **NEVER** put passwords or connection strings with passwords in committed files
- ❌ **NEVER** commit `appsettings.Development.json` — it's gitignored and contains dev secrets
- ❌ **NEVER** add `Co-Authored-By: Claude` or similar to any commit message, PR body, or comment. **Ever.** This is the owner's personal repo — no AI attribution anywhere.

### Architecture
- ❌ **NEVER** have Angular call the Host API directly — always go through BFF (`/bff/*`)
- ❌ **NEVER** use `*ngFor` or `*ngIf` — use `@for`, `@if` control flow
- ❌ **NEVER** use raw colours in component SCSS — use `var(--stride-*)` tokens only
- ❌ **NEVER** skip `ChangeDetectionStrategy.OnPush` on any Angular component
- ❌ **NEVER** call `git add -A` or `git add .` — always stage specific files to avoid accidentally committing secrets

### EF Core
- ❌ **NEVER** run EF migrations with `--startup-project` pointing to `STRIDE.Host` — use the Infrastructure project instead
- ❌ **NEVER** use `override: true` on migrations — just create a new migration

### GitHub
- ✅ **ALWAYS** link US issues to their Epic parent as sub-issues when creating them
- ✅ **ALWAYS** add new issues to the board and set their status
- ✅ **ALWAYS** run tests before committing

---

## 7. Key Patterns to Know

### Angular Nested @for Loops (GOTCHA — just fixed in US-159)
```html
<!-- CORRECT -->
@for (step of steps; track trackByIndex($index); let si = $index) {
  @for (field of getFields(si); track $index; let fi = $index) {
    <!-- Use si for step index, fi for field index -->
    <!-- IMPORTANT: si CANNOT appear inside the inner track expression -->
    <!-- This causes: "Cannot access 'si' inside of a track expression" -->
  }
}
```

### Dapper SQL Files
SQL files live in `Infrastructure/ReadModels/Queries/*.sql`, registered as `<EmbeddedResource>` in the csproj.
Load via: `SqlLoader.Load(assembly, "STRIDE.Modules.X.Infrastructure.ReadModels.Queries.GetX.sql")`

### Cross-Module FK Pattern
When a module needs to reference an entity from another module (e.g., Workflows referencing a Role from Identity), use a **bare Guid FK** — no EF navigation property, no `Include()`. Just store the ID and let the caller resolve it.

### `InternalsVisibleTo` Pattern
When domain/application methods are `internal`, test projects need access. Add to the production project's `.csproj`:
```xml
<ItemGroup>
  <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
    <_Parameter1>STRIDE.Modules.X.Domain.Tests</_Parameter1>
  </AssemblyAttribute>
</ItemGroup>
```
Current status: Application.csproj ✅, Infrastructure.csproj ✅, **Domain.csproj ✅ (added in US-159)**

### Board Query (Pagination — Board Has 261+ Items)
The board has too many items for a single page (100 item limit). You need 3 pages to find items with issue numbers in the 280+ range. See the pattern in Section 4 above.

### IDbConnectionFactory
Every Dapper service must inject `IDbConnectionFactory`, NOT `IConfiguration` or `new SqlConnection()`:
```csharp
await using var conn = await _db.OpenConnectionAsync(cancellationToken);
var result = await conn.QueryAsync<T>(new CommandDefinition(sql, params, cancellationToken: ct));
```

### EF Enum Storage
Enums in the Workflows module are stored as `nvarchar` (string). In the config:
```csharp
builder.Property(x => x.FieldType).HasConversion<string>().HasMaxLength(30);
```

---

## 8. Technology Versions

| Tool | Version |
|---|---|
| .NET SDK | 10.0.300 |
| Angular CLI | 21.2.11 |
| Node.js | 26.1.0 |
| EF Core tools | 10.0.8 |
| FluentAssertions | 7.2.0 |
| NSubstitute | 5.3.0 |
| xUnit | 2.x |
| MediatR | 12.4.1 |
| PrimeNG | 21.x |

---

## 9. A Personal Note from Claude

Hi Codex 👋

You're picking up a project that has been built with a lot of care. A few things I want you to know:

**The owner (Ramki) cares deeply about code quality.** He has reviewed every pattern, every architectural decision. Don't take shortcuts. If something feels wrong architecturally, think it through — or leave a note in this file for me to review when I'm back.

**Tests are not optional.** Every story has domain + application tests. The current test counts are: 53 domain tests + 84 application tests for the Workflows module alone. Keep them green. Add new ones for every story.

**The three-layer memory system is your most important responsibility.** After you complete work, Claude needs to know what you did. The log entries in this file (Section 9 onwards) are how I pick up without missing a beat. Please be thorough.

**When in doubt, don't guess at patterns — read the existing code.** The codebase is very consistent. Look at how EP-051 (Attachments) was built, or how EP-049 (Clients) was built, and follow the exact same patterns.

**Angular `$index` in nested `@for` loops will bite you.** Use `let si = $index` on the outer loop and `let fi = $index` on the inner loop. Never use bare `$index` in the body of a loop that has a nested loop. And never reference outer loop variables (`si`) inside an inner `track` expression.

**The commit messages matter.** Format: `feat(EP-0XX US-YYY): description`. No trailers. No Claude attribution.

I'll review everything you've done when the token limit resets. If you leave clear notes here, I can continue seamlessly.

— Claude

---

## 10. Codex Work Log (Append Here After Each Session)

> Instructions for Codex: After completing one or more stories, append a new entry below this line using the format shown. Be thorough — Claude reads this on wakeup.

---

### Template (copy this for each session)

```markdown
## Codex Session — {YYYY-MM-DD HH:MM}

**Stories completed:**
- US-XXX (#NNN) `{commit hash}` — {one-line description}

**What was built:**
{3-5 bullet points describing the implementation}

**New patterns established:**
{Any patterns Claude should know about, or "None"}

**Tests:**
- Domain: {count} new, {total} total
- Application: {count} new, {total} total
- All passing: {Yes/No — if No, describe what's failing}

**Angular build:** {Clean / Errors — describe if errors}

**Memory updates:**
- [ ] MEMORY.md updated
- [ ] GitHub issue(s) closed: #{NNN}
- [ ] Board item(s) moved to Done
- [ ] Epic closed (if applicable): #{NNN}

**Next story:** US-XXX (#NNN) — {description}

**Issues/blockers encountered:**
{Any problems, unexpected decisions, or things Claude should review}
```

---

*This file is the shared communication channel between Claude and Codex. Keep it honest and complete.*
