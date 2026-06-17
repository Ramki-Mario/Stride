#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# StrydeSuite — One-time Azure Container Apps infrastructure setup
#
# Run this ONCE from your local machine after authenticating:
#   az login
#   export DB_CONNECTION="Server=tcp:...;..."
#   export REDIS_CONNECTION="rediss://..."
#   export JWT_SECRET="your-signing-key"
#   export GHCR_USERNAME="ramki-mario"
#   export GHCR_TOKEN="ghp_..."        # GitHub PAT with read:packages
#   bash infra/setup.sh
#
# After running:
#   1. Copy the BFF FQDN printed at the end.
#   2. In your DNS provider, add:
#        CNAME  api  →  <bff-fqdn>
#   3. In Azure Portal → strydesuite-bff → Custom Domains, bind api.strydesuite.com.
#      Azure provisions the managed TLS cert automatically.
#   4. Add GitHub Actions secrets (see list at bottom of this script).
# ─────────────────────────────────────────────────────────────────────────────
set -euo pipefail

RG="strydesuite-free-prod-resource-group"
LOCATION="centralindia"
ENV="strydesuite-env"
HOST_APP="strydesuite-host"
BFF_APP="strydesuite-bff"

# ── Azure SQL (already created manually) ────────────────────────────────────
# Server:   stryde-suite-db-server.database.windows.net  (Central India)
# Database: stryde-suite-db
# User:     founder
# DB_CONNECTION format:
#   Server=tcp:stryde-suite-db-server.database.windows.net,1433;Database=stryde-suite-db;
#   User Id=founder;Password=<pass>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;

# ── Validate required env vars ───────────────────────────────────────────────
for var in DB_CONNECTION REDIS_CONNECTION JWT_SECRET GHCR_USERNAME GHCR_TOKEN; do
  [[ -z "${!var:-}" ]] && { echo "ERROR: \$$var is not set."; exit 1; }
done

echo "==> Resource group: $RG ($LOCATION)"
az group create --name "$RG" --location "$LOCATION" --output none

echo "==> Container Apps environment: $ENV"
az containerapp env create \
  --name "$ENV" \
  --resource-group "$RG" \
  --location "$LOCATION" \
  --output none

# Internal DNS suffix used for service-to-service calls within the environment.
ENV_DOMAIN=$(az containerapp env show \
  --name "$ENV" \
  --resource-group "$RG" \
  --query properties.defaultDomain -o tsv)

HOST_INTERNAL_URL="http://${HOST_APP}.internal.${ENV_DOMAIN}"
echo "    Host internal URL will be: $HOST_INTERNAL_URL"

# ── strydesuite-host (internal ingress — never publicly reachable) ────────────
echo "==> Creating $HOST_APP (internal ingress)"
az containerapp create \
  --name "$HOST_APP" \
  --resource-group "$RG" \
  --environment "$ENV" \
  --image "mcr.microsoft.com/dotnet/aspnet:10.0" \
  --target-port 8080 \
  --ingress internal \
  --transport http \
  --cpu 0.25 \
  --memory 0.5Gi \
  --min-replicas 0 \
  --max-replicas 2 \
  --registry-server ghcr.io \
  --registry-username "$GHCR_USERNAME" \
  --registry-password "$GHCR_TOKEN" \
  --secrets \
    "db-connection=${DB_CONNECTION}" \
    "redis-connection=${REDIS_CONNECTION}" \
    "jwt-secret=${JWT_SECRET}" \
  --env-vars \
    ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    "ConnectionStrings__DefaultConnection=secretref:db-connection" \
    "Redis__ConnectionString=secretref:redis-connection" \
    "Jwt__Secret=secretref:jwt-secret" \
    Jwt__Issuer=StrydeSuite \
    Jwt__Audience=StrydeSuite.Clients \
  --output none

# ── strydesuite-bff (external ingress — public at api.strydesuite.com) ────────
echo "==> Creating $BFF_APP (external ingress)"
az containerapp create \
  --name "$BFF_APP" \
  --resource-group "$RG" \
  --environment "$ENV" \
  --image "mcr.microsoft.com/dotnet/aspnet:10.0" \
  --target-port 8080 \
  --ingress external \
  --transport http \
  --cpu 0.25 \
  --memory 0.5Gi \
  --min-replicas 0 \
  --max-replicas 2 \
  --registry-server ghcr.io \
  --registry-username "$GHCR_USERNAME" \
  --registry-password "$GHCR_TOKEN" \
  --secrets \
    "redis-connection=${REDIS_CONNECTION}" \
  --env-vars \
    ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    "Services__StrideHost=${HOST_INTERNAL_URL}" \
    Cors__SpaOrigin=https://app.strydesuite.com \
    "Redis__ConnectionString=secretref:redis-connection" \
  --output none

BFF_FQDN=$(az containerapp show \
  --name "$BFF_APP" \
  --resource-group "$RG" \
  --query properties.configuration.ingress.fqdn -o tsv)

# ── GitHub Actions service principal ────────────────────────────────────────
echo "==> Creating service principal for GitHub Actions"
SUBSCRIPTION_ID=$(az account show --query id -o tsv)
SP_JSON=$(az ad sp create-for-rbac \
  --name "strydesuite-github-actions" \
  --role Contributor \
  --scopes "/subscriptions/${SUBSCRIPTION_ID}/resourceGroups/${RG}" \
  --sdk-auth \
  --output json)

echo ""
echo "╔══════════════════════════════════════════════════════════════════════╗"
echo "║                        SETUP COMPLETE                               ║"
echo "╠══════════════════════════════════════════════════════════════════════╣"
echo "║  BFF public FQDN: $BFF_FQDN"
echo "╠══════════════════════════════════════════════════════════════════════╣"
echo "║  NEXT STEPS                                                          ║"
echo "║  1. DNS: CNAME  api  →  $BFF_FQDN"
echo "║  2. Azure Portal → strydesuite-bff → Custom Domains                  ║"
echo "║     → bind api.strydesuite.com (managed cert auto-provisioned)       ║"
echo "╠══════════════════════════════════════════════════════════════════════╣"
echo "║  GitHub Actions secrets to add (Settings → Secrets → Actions):       ║"
echo "║                                                                       ║"
echo "║  AZURE_CREDENTIALS  →  (see JSON below)                              ║"
echo "║  GHCR_TOKEN         →  same PAT you used above (read:packages)        ║"
echo "╚══════════════════════════════════════════════════════════════════════╝"
echo ""
echo "=== AZURE_CREDENTIALS (paste this entire block as the secret value) ==="
echo "$SP_JSON"
echo "========================================================================"
