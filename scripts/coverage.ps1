#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Run all backend unit tests and generate an HTML coverage report.

.DESCRIPTION
    1. Runs dotnet test with Coverlet data collection
    2. Merges all coverage XML files using ReportGenerator
    3. Opens the HTML report in the default browser

.EXAMPLE
    .\scripts\coverage.ps1
    .\scripts\coverage.ps1 -NoBrowser      # generate without opening browser
    .\scripts\coverage.ps1 -Filter "Workflows"  # only run Workflows test projects

.NOTES
    Requires: dotnet-reportgenerator-globaltool
    Install:  dotnet tool install -g dotnet-reportgenerator-globaltool
#>

param(
    [switch] $NoBrowser,
    [string] $Filter = ""
)

$ErrorActionPreference = "Stop"
$root        = Split-Path -Parent $PSScriptRoot
$backend     = Join-Path $root "backend"
$resultsDir  = Join-Path $backend "TestResults"
$reportDir   = Join-Path $resultsDir "CoverageReport"

# ── 1. Clean previous results ─────────────────────────────────────────────────
if (Test-Path $resultsDir) {
    Write-Host "Cleaning previous test results..." -ForegroundColor Cyan
    Remove-Item $resultsDir -Recurse -Force
}
New-Item -ItemType Directory -Path $resultsDir | Out-Null

# ── 2. Run tests with coverage collection ─────────────────────────────────────
Write-Host "`nRunning tests with coverage collection..." -ForegroundColor Cyan

$testArgs = @(
    "test", (Join-Path $backend "STRIDE.sln"),
    "--configuration", "Release",
    "--verbosity", "normal",
    "--logger", "trx;LogFileName=test-results.trx",
    "--results-directory", $resultsDir,
    "--collect:XPlat Code Coverage",
    "--",
    "DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura"
)

if ($Filter) {
    $testArgs += @("--filter", "FullyQualifiedName~$Filter")
}

& dotnet @testArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "`nTests FAILED — coverage report not generated." -ForegroundColor Red
    exit $LASTEXITCODE
}

# ── 3. Check ReportGenerator is installed ─────────────────────────────────────
$reportGen = Get-Command "reportgenerator" -ErrorAction SilentlyContinue
if (-not $reportGen) {
    Write-Host "`nreportgenerator not found. Installing..." -ForegroundColor Yellow
    dotnet tool install -g dotnet-reportgenerator-globaltool
}

# ── 4. Find coverage XML files ────────────────────────────────────────────────
$coverageFiles = Get-ChildItem -Path $resultsDir -Recurse -Filter "coverage.cobertura.xml"
if (-not $coverageFiles) {
    Write-Host "`nNo coverage files found in $resultsDir" -ForegroundColor Red
    exit 1
}

$coveragePattern = ($coverageFiles | Select-Object -ExpandProperty FullName) -join ";"
Write-Host "`nFound $($coverageFiles.Count) coverage file(s)." -ForegroundColor Green

# ── 5. Generate HTML report ───────────────────────────────────────────────────
Write-Host "`nGenerating HTML coverage report..." -ForegroundColor Cyan

reportgenerator `
    "-reports:$coveragePattern" `
    "-targetdir:$reportDir" `
    "-reporttypes:Html;HtmlSummary;Badges;TextSummary" `
    "-assemblyfilters:+STRIDE.*;-*.Tests" `
    "-classfilters:-*Migrations*;-*ModelSnapshot*;-*DbContextFactory*" `
    "-verbosity:Warning"

# ── 6. Print summary ──────────────────────────────────────────────────────────
$summaryFile = Join-Path $reportDir "Summary.txt"
if (Test-Path $summaryFile) {
    Write-Host "`n────────────────────────── Coverage Summary ──────────────────────────" -ForegroundColor Green
    Get-Content $summaryFile | Where-Object { $_ -match "Line coverage|Branch coverage|Method coverage|Class:" } | ForEach-Object {
        Write-Host "  $_" -ForegroundColor White
    }
    Write-Host "──────────────────────────────────────────────────────────────────────" -ForegroundColor Green
}

Write-Host "`nReport saved to: $reportDir" -ForegroundColor Green
$indexHtml = Join-Path $reportDir "index.html"

# ── 7. Open in browser (unless suppressed) ────────────────────────────────────
if (-not $NoBrowser -and (Test-Path $indexHtml)) {
    Write-Host "Opening report in browser..." -ForegroundColor Cyan
    Start-Process $indexHtml
}

Write-Host "`nDone." -ForegroundColor Green
