# STRIDE — Development Workflow

> Read this file at the start of any session to re-orient immediately.
> This document defines how work is tracked, branched, and delivered.

---

## GitHub Setup (Already Complete)

| Resource | URL |
|---|---|
| Repository | https://github.com/Ramki-Mario/Stride |
| Project Board | https://github.com/users/Ramki-Mario/projects/2 |
| Default Branch | `develop` |

**GitHub CLI path on this machine:**
```
C:\Program Files\GitHub CLI\gh.exe
```
Always use the full path in PowerShell — `gh` is not in the default PATH.

---

## Branch Strategy

```
develop          ← integration branch (default)
  └── feature/us-{ID}-{short-description}   ← one branch per user story
  └── fix/{short-description}               ← bug fixes
  └── chore/{short-description}             ← tooling, config, docs
```

**Rules:**
- Never commit directly to `develop`
- One feature branch per user story
- Branch name must include the story ID: `feature/us-018-identity-domain`
- Merge back to `develop` via PR when story is done
- Delete the feature branch after merge

---

## Story Lifecycle (End-to-End)

When the user says **"Work on US-XXX"**:

1. **Read context** — `current-status.md`, `sprint.md`, relevant agent files
2. **Create branch** — `git checkout -b feature/us-XXX-description develop`
3. **Implement** — follow architecture rules from agent files
4. **Commit** — one or more commits on the feature branch
5. **Raise PR** — `gh pr create` targeting `develop`
6. **Update GitHub Issue** — close the issue, add a comment summarizing what was done
7. **Update sprint.md** — mark the story `[x]` Done
8. **Update current-status.md** — if it's a significant milestone

---

## Issue Number Map (Sprint 2)

| Issue | ID | Title | Status |
|---|---|---|---|
| #8 | EP-008 | Identity Domain Model | Backlog |
| #9 | EP-009 | Tenant Domain Model | Backlog |
| #10 | EP-010 | Identity Infrastructure — EF Core and Migrations | Backlog |
| #11 | EP-011 | JWT Strategy and Internal Auth Provider | Backlog |
| #12 | EP-012 | Identity Application Layer — Commands and Queries | Backlog |
| #13 | EP-013 | Identity API Layer | Backlog |
| #14 | EP-014 | RBAC and Claims Authorization | Backlog |
| #15 | EP-015 | Angular Login UI | Backlog |
| #16 | EP-016 | appsettings.Development.json | Backlog |
| #17 | US-018 | User, Role, Permission entities | Done ✅ PR #32 |
| #18 | US-019 | Password value object | Done ✅ PR #32 |
| #19 | US-020 | Tenant, TenantDomainMapping, UserTenantMapping | Done ✅ PR #33 |
| #31 | US-021 | Tenant resolver | Done ✅ PR #34 |
| #20 | US-022 | EF Core configurations and initial migration | Backlog |
| #21 | US-023 | Repository implementations | Backlog |
| #22 | US-024 | BFF auth endpoints — login, logout, me | Backlog |
| #23 | US-025 | JWT configuration via appsettings | Backlog |
| #24 | US-026 | MediatR command and query handlers | Backlog |
| #25 | US-027 | FluentValidation validators for commands | Backlog |
| #26 | US-028 | Identity API controllers | Backlog |
| #27 | US-029 | Claims-based authorization policies | Backlog |
| #28 | US-030 | Angular login page | Backlog |
| #29 | US-031 | Auth guard and APP_INITIALIZER | Backlog |
| #30 | US-032 | appsettings.Development.json for local dev | Backlog |

---

## Sprint 2 Dependency Order

Work must be done in this order — each depends on the previous:

```
US-018 (User/Role/Permission entities)
US-019 (Password value object)
  └── US-020 (Tenant entities)
        └── US-021 (Tenant resolver)
              └── US-022 (EF Core + migrations)
                    └── US-023 (Repositories)
                          └── US-025 (JWT config)
                                └── US-026 (MediatR handlers)
                                      └── US-027 (FluentValidation)
                                            └── US-028 (Identity API controllers)
                                                  └── US-024 (BFF auth endpoints)
                                                        └── US-029 (RBAC policies)
                                                              ├── US-030 (Angular login page)
                                                              ├── US-031 (Auth guard)
                                                              └── US-032 (appsettings.Development.json)
```

---

## Updating a GitHub Issue When Done

```powershell
$gh = "C:\Program Files\GitHub CLI\gh.exe"

# Close issue and add completion comment
& $gh issue close {NUMBER} --repo Ramki-Mario/Stride --comment "Implemented in branch feature/us-XXX-description. Merged to develop via PR #{PR_NUMBER}."

# Move to Done on project board (use scripts/set-issue-done.ps1)
```

---

## PR Template

Every PR to `develop` must include:
- Title: `feat(us-XXX): short description`
- Body: what was implemented, architectural decisions made, how to test
- Linked issue: `Closes #XX`

---

## Re-Entry Protocol (Start of Every Session)

If context was lost, read these files in order:

1. `pilot/current-status.md` — what phase, what's done, what's next
2. `pilot/sprint.md` — story-level status (look for `[-]` In Progress)
3. `pilot/workflow.md` — this file (branching, issue map)
4. `pilot/agents/project-rules.agent.md` — non-negotiable rules
5. Relevant agent file for the module being worked on

Then check GitHub:
```powershell
$gh = "C:\Program Files\GitHub CLI\gh.exe"
# What's in progress on the board?
& $gh project item-list 2 --owner Ramki-Mario --format json | ConvertFrom-Json | `
  Where-Object { $_.status -eq "In Progress" }
# What branches exist?
git branch -a
```

These two commands tell you exactly what was being worked on.
