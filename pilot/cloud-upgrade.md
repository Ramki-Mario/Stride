# Cloud Upgrade Plan — First Real Tenant

> **When to use this file:** When the first paying tenant signs up, execute this plan in order.
> Pre-conditions: contract signed, payment method confirmed, go-live date agreed.

---

## Current Pre-Launch Stack (FREE — $0/month)

| Layer | Service | Tier | Notes |
|---|---|---|---|
| Angular SPA | Azure Static Web Apps | Free (permanent) | Auto-deploys from GitHub main/develop |
| STRIDE.Host + STRIDE.BFF | Azure App Service F1 | Free (1GB RAM, 60 CPU min/day) | UptimeRobot ping every 14 min to prevent sleep |
| Database | Azure SQL Database | Free serverless (100k vCore-s/month) | Dev/demo only — no SLA |
| Redis | Redis Cloud free | 30MB free | `redis-18113.crce300.ap-south-1-2.ec2.cloud.redislabs.com:18113` |
| Email | Resend.com | Free (100 emails/day) | |
| File storage | Local file system | — | `Storage:Provider = local` in appsettings |
| CI/CD | GitHub Actions | Free (2,000 min/month) | |

---

## Upgrade Steps (execute when Tenant #1 signs)

### Step 1 — Upgrade App Service: F1 → B1

```
Azure Portal → App Services → STRIDE-AppServicePlan → Scale Up → B1 (Basic)
```

- **Cost delta:** +$13.14/month
- **What changes:** 1 core → 1 core (same), but no CPU quota, always-on, custom domains, SLA 99.95%
- Remove UptimeRobot ping (no longer needed — always-on enabled)
- Verify both Host and BFF are on the same B1 plan (they share it)

### Step 2 — Upgrade Azure SQL: Serverless → Basic

```
Azure Portal → SQL Databases → STRIDE-db → Configure → Basic (5 DTU, 2GB)
```

- **Cost delta:** +$4.90/month
- **What changes:** Serverless auto-pauses → Basic is always-on, no cold-start delays
- Run all pending EF migrations immediately after upgrade:
  ```powershell
  dotnet ef database update --project src/Modules/Identity/... --startup-project src/Host/STRIDE.Host
  # Repeat for each module with pending migrations
  ```

### Step 3 — Redis: Assess Free Tier Usage

- Check current Redis memory usage in Redis Cloud dashboard
- **If < 20MB used:** Stay on free tier — no action needed
- **If > 20MB used:** Upgrade Redis Cloud to Essentials 100MB plan (~$7/month) OR switch to Azure Cache for Redis C0 (~$16/month)
- Redis Cloud is preferred (cheaper, already configured)

### Step 4 — Switch File Storage: Local → Azure Blob

```
Azure Portal → Storage Accounts → Create → STRIDE-storage → LRS Hot
```

1. Create container `stride-attachments` (private)
2. Generate connection string
3. Update `appsettings.json` (Host):
   ```json
   "Storage": { "Provider": "azure", "ConnectionString": "<your-connection-string>", "ContainerName": "stride-attachments" }
   ```
4. Migrate any existing local files to Blob (one-time script in `scripts/migrate-attachments.ps1`)
- **Cost:** ~$0.20/month per 10GB — negligible

### Step 5 — Email: Resend.com Free → Paid (if needed)

- Free tier: 100 emails/day
- At 50 users with moderate activity, 100/day is likely sufficient initially
- Upgrade to Resend Starter ($20/month, 50k emails) only when you consistently hit the daily cap
- Check Resend dashboard → Logs for daily volume

### Step 6 — Update Environment Variables / appsettings

Update in Azure App Service → Configuration → Application Settings:

| Key | Value |
|---|---|
| `ConnectionStrings__DefaultConnection` | New SQL connection string (if changed) |
| `Storage__Provider` | `azure` |
| `Storage__ConnectionString` | Azure Blob connection string |
| `Vapid__PrivateKey` | *(from your local `appsettings.Development.json` — never commit this)* |
| `Email__ApiKey` | Resend API key |

> ⚠️ VAPID private key and all secrets go in App Service Application Settings (encrypted at rest), never in committed appsettings files.

### Step 7 — Verify Production Readiness

- [ ] Run health check: `GET https://your-host.azurewebsites.net/health` → 200 OK
- [ ] Run BFF health check: `GET https://your-bff.azurewebsites.net/health` → 200 OK
- [ ] Login flow end-to-end: register tenant → login → view dashboard
- [ ] Create a workflow and complete a step
- [ ] Check push notification delivery
- [ ] Check email notification delivery
- [ ] Verify file upload (attachment) to Azure Blob
- [ ] Run EF migrations on the production DB (confirm no pending migrations)
- [ ] Confirm Redis session is persisting (login → refresh → still logged in)

---

## Post-Upgrade Cost Summary

| Service | Monthly |
|---|---|
| Azure App Service B1 (Host + BFF) | $13.14 |
| Azure SQL Basic (2GB) | $4.90 |
| Redis Cloud 30MB (free) | $0 |
| Azure Static Web Apps | $0 |
| Azure Blob Storage (~10GB) | $0.20 |
| Resend.com (free tier) | $0 |
| GitHub Actions CI/CD | $0 |
| **Total** | **~$18–25/month** |

**Revenue at $299/month → Net after infra: ~$274/month (92% gross margin)**

---

## Scaling Triggers (future — no action now)

| Trigger | Action |
|---|---|
| > 200 concurrent users | Scale App Service B1 → B2 (+$26/month) |
| > 5GB DB storage | SQL Basic → Standard S1 10 DTU (+$15/month) |
| Redis hitting 25MB free cap | Redis Cloud Essentials 100MB (+$7/month) |
| Email > 100/day consistently | Resend Starter plan (+$20/month) |
| Need guaranteed notification delivery | Add Azure Service Bus Basic (+$10/month) and migrate IWebPushService/IEmailService to queue-based delivery |
| > 3 tenants | Consider Azure App Service B2 or P1v3 for multi-tenant headroom |
