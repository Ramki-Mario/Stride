# StrydeSuite — SQL Deployment Guide

Run this when you need to apply EF migrations to Azure SQL (first-time setup, new module added, or model change).

---

## Prerequisites (one-time, already done)

- `dotnet-ef` tool installed globally
- Azure SQL server `stryde-suite-db-server` created
- Your machine's IP whitelisted in Azure SQL firewall

If you get a **firewall error**, see [Fix: IP not whitelisted](#fix-ip-not-whitelisted) below.

---

## Steps

### 1. Open PowerShell — normal mode, no admin needed

Win + X → **Terminal** (or search "PowerShell").\
No admin / "Run as Administrator" required.

### 2. Navigate to the project root

```powershell
cd D:\Repos\stride
```

You should see `D:\Repos\stride>` in the prompt before continuing.

### 3. Set your Azure SQL password

```powershell
$env:DB_PASSWORD = "your-founder-password"
```

Replace `your-founder-password` with the actual password for the `founder` account on `stryde-suite-db-server`.

### 4. Run the migration script

```powershell
powershell -ExecutionPolicy Bypass -File .\infra\migrate.ps1
```

The script runs all 11 modules in order and prints PASS / FAIL per module.\
A successful run ends with:

```
All 11 modules migrated successfully.
Next step: run infra/setup.sh to create the Azure Container Apps.
```

---

## What the script targets

| Setting | Value |
|---|---|
| Server | `stryde-suite-db-server.database.windows.net` |
| Database | `stryde-suite-db` |
| User | `founder` |
| Password | from `$env:DB_PASSWORD` |
| Region | Central India |

---

## Troubleshooting

### Fix: IP not whitelisted

Error message: `Client with IP address 'x.x.x.x' is not allowed to access the server`

1. Go to [Azure Portal](https://portal.azure.com)
2. SQL servers → `stryde-suite-db-server` → **Networking**
3. Under Firewall rules, click **Add your client IP** (auto-detects your current IP)
4. Click **Save**
5. Wait ~1 minute, then re-run the script

If your ISP rotates your IP frequently, add a range instead:
- Rule name: `devmachine`
- Start IP: `103.160.240.0`
- End IP: `103.160.240.255`

### Fix: PendingModelChangesWarning on a module

Error message: `The model for context 'XyzDbContext' has pending changes. Add a new migration before updating the database.`

This means a domain entity was changed but no migration was generated for it. Run:

```powershell
dotnet ef migrations add SyncModel --project backend/src/Modules/<ModuleName>/STRIDE.Modules.<ModuleName>.Infrastructure --context <ModuleName>DbContext
```

Replace `<ModuleName>` with the failing module name (e.g. `Identity`, `Clients`, `KitOps`).

Then commit the generated files and re-run the migrate script.

### Fix: dotnet-ef not found

```powershell
dotnet tool install --global dotnet-ef
```

Then close and reopen PowerShell (so the PATH refreshes), and re-run.

### Fix: Only some modules failed (others passed)

Safe to re-run the full script — EF skips modules that are already up to date.\
Fix the root cause first (firewall or pending migration), then re-run.

---

## Module migration order

The script always runs in this order (Identity first — other modules have FK dependencies on it):

1. Identity
2. Administration
3. Workflows
4. Scheduling
5. Clients
6. Teams
7. Invoicing
8. Reporting
9. Notifications
10. Webhooks
11. KitOps

---

## After migrations are done

Next step is creating the Azure Container Apps. See `pilot/deployment-container-apps.md`.
