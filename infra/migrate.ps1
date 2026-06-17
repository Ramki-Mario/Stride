# -----------------------------------------------------------------------------
# StrydeSuite -- Apply all EF Core migrations to Azure SQL
#
# Usage (run from repo root):
#   $env:DB_PASSWORD = "your-founder-password"
#   powershell -ExecutionPolicy Bypass -File .\infra\migrate.ps1
#
# Or pass the full connection string directly:
#   powershell -ExecutionPolicy Bypass -File .\infra\migrate.ps1 -ConnectionString "Server=tcp:stryde-suite-db-server..."
#
# Prerequisite -- install EF Core tools if not already:
#   dotnet tool install --global dotnet-ef
# -----------------------------------------------------------------------------
param(
    [string]$ConnectionString = ""
)

$ErrorActionPreference = "Stop"

# -- Build connection string --------------------------------------------------
if (-not $ConnectionString) {
    $password = $env:DB_PASSWORD
    if (-not $password) {
        $password = Read-Host -Prompt "Azure SQL password for 'founder'"
    }
    $ConnectionString = "Server=tcp:stryde-suite-db-server.database.windows.net,1433;" +
                        "Database=stryde-suite-db;" +
                        "User Id=founder;" +
                        "Password=$password;" +
                        "Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
}

$env:ConnectionStrings__DefaultConnection = $ConnectionString
Write-Host ""
Write-Host "Target: stryde-suite-db-server.database.windows.net / stryde-suite-db" -ForegroundColor Cyan
Write-Host ""

# -- Modules to migrate (order matters -- Identity first for FK deps) ---------
$modules = @(
    "backend/src/Modules/Identity/STRIDE.Modules.Identity.Infrastructure",
    "backend/src/Modules/Administration/STRIDE.Modules.Administration.Infrastructure",
    "backend/src/Modules/Workflows/STRIDE.Modules.Workflows.Infrastructure",
    "backend/src/Modules/Scheduling/STRIDE.Modules.Scheduling.Infrastructure",
    "backend/src/Modules/Clients/STRIDE.Modules.Clients.Infrastructure",
    "backend/src/Modules/Teams/STRIDE.Modules.Teams.Infrastructure",
    "backend/src/Modules/Invoicing/STRIDE.Modules.Invoicing.Infrastructure",
    "backend/src/Modules/Reporting/STRIDE.Modules.Reporting.Infrastructure",
    "backend/src/Modules/Notifications/STRIDE.Modules.Notifications.Infrastructure",
    "backend/src/Modules/Webhooks/STRIDE.Modules.Webhooks.Infrastructure",
    "backend/src/Modules/KitOps/STRIDE.Modules.KitOps.Infrastructure"
)

$passed = 0
$failed = @()

foreach ($project in $modules) {
    $name = Split-Path $project -Leaf
    Write-Host "-- $name" -NoNewline

    try {
        $output = dotnet ef database update --project $project 2>&1
        if ($LASTEXITCODE -ne 0) { throw $output }
        Write-Host "  PASS" -ForegroundColor Green
        $passed++
    }
    catch {
        Write-Host "  FAIL" -ForegroundColor Red
        Write-Host $_ -ForegroundColor Red
        $failed += $name
    }
}

Write-Host ""
if ($failed.Count -eq 0) {
    Write-Host "All $passed modules migrated successfully." -ForegroundColor Green
    Write-Host ""
    Write-Host "Next step: run infra/setup.sh to create the Azure Container Apps." -ForegroundColor Cyan
} else {
    Write-Host "$passed passed, $($failed.Count) failed:" -ForegroundColor Yellow
    $failed | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    exit 1
}
