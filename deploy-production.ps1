#Requires -Version 7.0
<#
.SYNOPSIS
    KOAFiloServis Puantaj Engine production deployment.
    Timeouts: migration 10m, deployment 15m, /readyz 120s.
    Rollback: restore previous artifact + config.

.PARAMETER TargetPath
    IIS app path, default: C:\KOAFiloServis

.PARAMETER AppPoolName
    IIS AppPool name, default: KOAFiloServis

.PARAMETER HealthUrl
    Health check base URL, default: http://localhost:5190

.PARAMETER SkipMigration
    Skip EF migration step
#>

param(
    [string]$TargetPath = "C:\KOAFiloServis",
    [string]$AppPoolName = "KOAFiloServis",
    [string]$HealthUrl = "http://localhost:5190",
    [switch]$SkipMigration
)

$ErrorActionPreference = "Stop"
$DeployReport = @{
    StartedAt       = (Get-Date -Format "yyyy-MM-dd HH:mm:ss K")
    GitSHA          = ""
    Environment     = "Production"
    Migrations      = @()
    HealthResults   = @{}
    SmokeResults    = @{}
    RollbackStatus  = "Not needed"
    CompletedAt     = ""
    Verdict         = ""
}

$ReportPath = Join-Path $PSScriptRoot "deploy-report.md"
$BackupPath = Join-Path $PSScriptRoot "rollback-backup"
$PublishPath = Join-Path $PSScriptRoot "publish"

# ═══════════════════════════════════════════════════════════════════
# PHASE 0: PRE-FLIGHT
# ═══════════════════════════════════════════════════════════════════

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host " KOAFiloServis Production Deployment  " -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "[0/6] Pre-flight checks..." -ForegroundColor Yellow

# Git SHA
$DeployReport.GitSHA = git rev-parse HEAD 2>&1
Write-Host "  Git SHA: $($DeployReport.GitSHA)" -ForegroundColor Gray

# Git clean?
$gitStatus = git status --porcelain 2>&1
if ($gitStatus -match "^\s*[MADRCU]") {
    Write-Host "  GIT DIRTY! Uncommitted changes:" -ForegroundColor Red
    Write-Host $gitStatus
    throw "Git working tree is not clean. Commit or stash changes before deploy."
}
Write-Host "  Git status: clean" -ForegroundColor Green

# Release build
Write-Host "  Building Release..." -ForegroundColor Gray
dotnet build -c Release --no-restore 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Build failed" }
Write-Host "  Build: OK" -ForegroundColor Green

# Tests
Write-Host "  Running tests..." -ForegroundColor Gray
$testResult = dotnet test -c Release --no-build 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host $testResult
    throw "Tests failed"
}
Write-Host "  Tests: OK ($(($testResult | Select-String 'Total: (\d+)').Matches.Groups[1].Value) total)" -ForegroundColor Green

# appsettings.Production.json has PuantajEngine section?
$prodConfig = Get-Content "KOAFiloServis.Web\appsettings.Production.json" -Raw | ConvertFrom-Json
if (-not $prodConfig.PuantajEngine.AutoProcess) {
    throw "PuantajEngine:AutoProcess section missing in appsettings.Production.json"
}
Write-Host "  Config: PuantajEngine:AutoProcess present" -ForegroundColor Green

# Publish
Write-Host "  Publishing..." -ForegroundColor Gray
Remove-Item -Recurse -Force $PublishPath -ErrorAction SilentlyContinue
dotnet publish KOAFiloServis.Web -c Release -o $PublishPath 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Publish failed" }
Write-Host "  Publish: OK" -ForegroundColor Green

Write-Host "[0/6] Pre-flight: PASSED" -ForegroundColor Green
Write-Host ""

# ═══════════════════════════════════════════════════════════════════
# PHASE 1: BACKUP CURRENT STATE
# ═══════════════════════════════════════════════════════════════════

Write-Host "[1/6] Backup current state..." -ForegroundColor Yellow

if (Test-Path $TargetPath) {
    Remove-Item -Recurse -Force $BackupPath -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force $BackupPath | Out-Null

    Write-Host "  Backing up $TargetPath → $BackupPath..." -ForegroundColor Gray
    Copy-Item -Recurse "$TargetPath\*" $BackupPath -ErrorAction Stop

    # Backup current config
    Copy-Item "$TargetPath\appsettings.Production.json" "$BackupPath\appsettings.Production.json" -Force

    Write-Host "  Backup: OK ($((Get-ChildItem $BackupPath).Count) files)" -ForegroundColor Green
} else {
    Write-Host "  Target path does not exist — first deploy, no backup needed" -ForegroundColor Yellow
}
Write-Host ""

# ═══════════════════════════════════════════════════════════════════
# PHASE 2: DATABASE MIGRATION (10m timeout)
# ═══════════════════════════════════════════════════════════════════

if (-not $SkipMigration) {
    Write-Host "[2/6] Database migration (timeout: 10m)..." -ForegroundColor Yellow

    $env:ASPNETCORE_ENVIRONMENT = "Production"
    $migrationTimer = [System.Diagnostics.Stopwatch]::StartNew()

    try {
        $job = Start-Job -Name "ef-migration" -ScriptBlock {
            param($projectDir)
            Set-Location $projectDir
            $env:ASPNETCORE_ENVIRONMENT = "Production"
            dotnet ef database update `
                --project KOAFiloServis.Web `
                --context ApplicationDbContext `
                -- --timeout 600 2>&1
        } -ArgumentList $PSScriptRoot

        $completed = Wait-Job -Name "ef-migration" -Timeout 600  # 10 minutes
        if (-not $completed) {
            Stop-Job -Name "ef-migration"
            throw "Migration timeout (10m exceeded)"
        }

        $migrationOutput = Receive-Job -Name "ef-migration"
        Remove-Job -Name "ef-migration"

        if ($LASTEXITCODE -ne 0 -and $LASTEXITCODE) {
            Write-Host $migrationOutput
            throw "Migration failed with exit code $LASTEXITCODE"
        }

        $migrationTimer.Stop()
        Write-Host "  Migration: OK ($($migrationTimer.Elapsed.TotalSeconds.ToString('F1'))s)" -ForegroundColor Green

        # Get migration list for report
        $DeployReport.Migrations = @(dotnet ef migrations list `
            --project KOAFiloServis.Web `
            --context ApplicationDbContext 2>&1 |
            Select-String -Pattern "(Pending|Applied)" |
            ForEach-Object { $_.Line.Trim() })

    } catch {
        $migrationTimer.Stop()
        Write-Host "  Migration: FAILED ($($migrationTimer.Elapsed.TotalSeconds.ToString('F1'))s)" -ForegroundColor Red
        Write-Host "  $_" -ForegroundColor Red
        throw
    }
} else {
    Write-Host "[2/6] Database migration: SKIPPED (--SkipMigration)" -ForegroundColor Yellow
}
Write-Host ""

# ═══════════════════════════════════════════════════════════════════
# PHASE 3: DEPLOY ARTIFACT (15m timeout)
# ═══════════════════════════════════════════════════════════════════

Write-Host "[3/6] Deploy artifact (timeout: 15m)..." -ForegroundColor Yellow

$deployTimer = [System.Diagnostics.Stopwatch]::StartNew()

try {
    # Stop AppPool
    Write-Host "  Stopping AppPool '$AppPoolName'..." -ForegroundColor Gray
    & "$env:SystemRoot\System32\inetsrv\appcmd.exe" stop apppool /apppool.name:$AppPoolName 2>&1 | Out-Null

    # Copy new files
    Write-Host "  Copying files to $TargetPath..." -ForegroundColor Gray
    New-Item -ItemType Directory -Force $TargetPath | Out-Null

    # Preserve existing config + data
    $preserveFiles = @(
        "appsettings.Production.json",
        "dbsettings.json"
    )
    $preserveDirs = @("logs", "uploads", "Backups", "data")

    $preservedData = @{}
    foreach ($f in $preserveFiles) {
        $src = Join-Path $TargetPath $f
        if (Test-Path $src) { $preservedData[$f] = Get-Content $src -Raw }
    }

    # Copy new build
    robocopy $PublishPath $TargetPath /MIR /NFL /NDL /NJH /NJS /nc /ns /np /MT:8
    if ($LASTEXITCODE -ge 8) { throw "robocopy failed with exit code $LASTEXITCODE" }

    # Restore preserved files (if they exist & are different from published)
    foreach ($f in $preserveFiles) {
        if ($preservedData.ContainsKey($f) -and $preservedData[$f]) {
            $target = Join-Path $TargetPath $f
            $preservedData[$f] | Set-Content $target -NoNewline
            Write-Host "  Preserved: $f" -ForegroundColor Gray
        }
    }

    # Start AppPool
    Write-Host "  Starting AppPool '$AppPoolName'..." -ForegroundColor Gray
    & "$env:SystemRoot\System32\inetsrv\appcmd.exe" start apppool /apppool.name:$AppPoolName 2>&1 | Out-Null

    $deployTimer.Stop()
    Write-Host "  Deploy: OK ($($deployTimer.Elapsed.TotalSeconds.ToString('F1'))s)" -ForegroundColor Green
} catch {
    $deployTimer.Stop()
    Write-Host "  Deploy: FAILED" -ForegroundColor Red
    Write-Host "  Initiating rollback..."
    Invoke-Rollback
    throw
}
Write-Host ""

# ═══════════════════════════════════════════════════════════════════
# PHASE 4: HEALTH CHECK (120s timeout → rollback)
# ═══════════════════════════════════════════════════════════════════

Write-Host "[4/6] Health check (timeout: 120s → rollback)..." -ForegroundColor Yellow

$healthTimer = [System.Diagnostics.Stopwatch]::StartNew()
$ready = $false
$attempt = 0
$maxAttempts = 24  # 24 × 5s = 120s

do {
    $attempt++
    Start-Sleep -Seconds 5

    try {
        $liveness = Invoke-RestMethod -Uri "$HealthUrl/healthz" -Method Get -TimeoutSec 3
        $readiness = Invoke-RestMethod -Uri "$HealthUrl/readyz" -Method Get -TimeoutSec 3
        $jobHealth = Invoke-RestMethod -Uri "$HealthUrl/health/puantaj-job" -Method Get -TimeoutSec 3

        Write-Host "  [$attempt/$maxAttempts] liveness=$($liveness) readiness=$($readiness.status) job=$($jobHealth.status)" -ForegroundColor Gray

        if ($readiness.status -eq "Healthy") {
            $ready = $true
            break
        }
    } catch {
        Write-Host "  [$attempt/$maxAttempts] Waiting... ($($_.Exception.Message))" -ForegroundColor DarkYellow
    }
} while ($attempt -lt $maxAttempts)

$healthTimer.Stop()
$DeployReport.HealthResults = @{
    Liveness    = $liveness
    Readiness   = $readiness
    JobHealth   = $jobHealth
    Attempts    = $attempt
    Duration    = $healthTimer.Elapsed.TotalSeconds
}

if (-not $ready) {
    Write-Host "  HEALTH CHECK FAILED after 120s — INITIATING ROLLBACK" -ForegroundColor Red
    Invoke-Rollback
    throw "/readyz not healthy within 120s"
}

Write-Host "  Health check: ALL GREEN ($($healthTimer.Elapsed.TotalSeconds.ToString('F1'))s)" -ForegroundColor Green
Write-Host ""

# ═══════════════════════════════════════════════════════════════════
# PHASE 5: SMOKE TEST
# ═══════════════════════════════════════════════════════════════════

Write-Host "[5/6] Smoke tests..." -ForegroundColor Yellow

$smokeResults = @{}

# 1. Swagger accessible
try {
    $swagger = Invoke-RestMethod -Uri "$HealthUrl/swagger" -Method Get -TimeoutSec 5
    $smokeResults["swagger"] = "OK"
    Write-Host "  Swagger: OK" -ForegroundColor Green
} catch {
    $smokeResults["swagger"] = "FAIL: $_"
    Write-Host "  Swagger: FAIL" -ForegroundColor Red
}

# 2. Blazor UI accessible
try {
    $ui = Invoke-WebRequest -Uri "$HealthUrl" -Method Get -TimeoutSec 10
    $smokeResults["ui"] = if ($ui.StatusCode -eq 200) { "OK (200)" } else { "FAIL: $($ui.StatusCode)" }
    Write-Host "  UI: $($smokeResults['ui'])" -ForegroundColor $(if($ui.StatusCode -eq 200){"Green"}else{"Red"})
} catch {
    $smokeResults["ui"] = "FAIL: $_"
    Write-Host "  UI: FAIL" -ForegroundColor Red
}

# 3. Job history page
try {
    $history = Invoke-WebRequest -Uri "$HealthUrl/puantaj/jobs" -Method Get -TimeoutSec 10
    $smokeResults["job_ui"] = if ($history.StatusCode -eq 200) { "OK (200)" } else { "FAIL" }
    Write-Host "  Job History UI: $($smokeResults['job_ui'])" -ForegroundColor Green
} catch {
    $smokeResults["job_ui"] = "FAIL: $_"
    Write-Host "  Job History UI: FAIL" -ForegroundColor Red
}

$DeployReport.SmokeResults = $smokeResults
Write-Host ""

# ═══════════════════════════════════════════════════════════════════
# PHASE 6: GENERATE REPORT
# ═══════════════════════════════════════════════════════════════════

Write-Host "[6/6] Generate deploy report..." -ForegroundColor Yellow

$DeployReport.CompletedAt = (Get-Date -Format "yyyy-MM-dd HH:mm:ss K")
$DeployReport.Verdict = "SUCCESS"

$reportContent = @"
# KOAFiloServis Production Deploy Report

| Field | Value |
|-------|-------|
| **Date** | $($DeployReport.StartedAt) — $($DeployReport.CompletedAt) |
| **Git SHA** | `$($DeployReport.GitSHA)` |
| **Environment** | $($DeployReport.Environment) |
| **Verdict** | **$($DeployReport.Verdict)** |

## Migrations Applied

$(($DeployReport.Migrations | ForEach-Object { "- $_" }) -join "`n")

## Health Check Results

| Endpoint | Status | Duration |
|----------|--------|----------|
| /healthz | $($DeployReport.HealthResults.Liveness) | - |
| /readyz | $($DeployReport.HealthResults.Readiness.status) | $($DeployReport.HealthResults.Duration.ToString('F1'))s |
| /health/puantaj-job | $($DeployReport.HealthResults.JobHealth.status) | - |

## Smoke Tests

| Test | Result |
|------|--------|
| Swagger | $($DeployReport.SmokeResults.swagger) |
| Blazor UI | $($DeployReport.SmokeResults.ui) |
| Job History | $($DeployReport.SmokeResults.job_ui) |

## Rollback Status

$($DeployReport.RollbackStatus)

## Configuration

- PuantajEngine:AutoProcess:Enabled = $($prodConfig.PuantajEngine.AutoProcess.Enabled)
- CronExpression = $($prodConfig.PuantajEngine.AutoProcess.CronExpression)
- StaleTimeoutMinutes = $($prodConfig.PuantajEngine.AutoProcess.StaleTimeoutMinutes)
- EngineTimeoutSeconds = $($prodConfig.PuantajEngine.AutoProcess.EngineTimeoutSeconds)

## Post-Deploy Verification

- [ ] Manual trigger: POST $HealthUrl/api/puantaj/jobs/process/2026/4
- [ ] Verify PuantajJobExecutions table has new records
- [ ] Verify Quartz scheduler log: "PuantajJob başladı"
- [ ] Wait for next cron trigger or manual trigger
- [ ] Check Grafana dashboard for metrics
"@

$reportContent | Set-Content $ReportPath -Encoding UTF8
Write-Host "  Report: $ReportPath" -ForegroundColor Green
Write-Host ""

Write-Host "=====================================" -ForegroundColor Green
Write-Host " DEPLOY COMPLETE — VERDICT: SUCCESS  " -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green

# ═══════════════════════════════════════════════════════════════════
# ROLLBACK FUNCTION
# ═══════════════════════════════════════════════════════════════════

function Invoke-Rollback {
    Write-Host ""
    Write-Host "=== ROLLBACK INITIATED ===" -ForegroundColor Red

    if (Test-Path $BackupPath) {
        # Stop AppPool
        & "$env:SystemRoot\System32\inetsrv\appcmd.exe" stop apppool /apppool.name:$AppPoolName 2>&1 | Out-Null

        # Restore backed up files
        Write-Host "  Restoring from $BackupPath → $TargetPath..." -ForegroundColor Yellow
        robocopy $BackupPath $TargetPath /MIR /NFL /NDL /NJH /NJS /nc /ns /np /MT:8

        # Start AppPool
        & "$env:SystemRoot\System32\inetsrv\appcmd.exe" start apppool /apppool.name:$AppPoolName 2>&1 | Out-Null

        # Verify rollback
        Start-Sleep -Seconds 10
        try {
            $rollbackHealth = Invoke-RestMethod -Uri "$HealthUrl/readyz" -Method Get -TimeoutSec 10
            if ($rollbackHealth.status -eq "Healthy") {
                Write-Host "  Rollback: SUCCESS — /readyz Healthy" -ForegroundColor Green
                $DeployReport.RollbackStatus = "SUCCESS — restored to $($DeployReport.GitSHA)"
            } else {
                Write-Host "  Rollback verified but /readyz: $($rollbackHealth.status)" -ForegroundColor Yellow
                $DeployReport.RollbackStatus = "PARTIAL — /readyz=$($rollbackHealth.status)"
            }
        } catch {
            Write-Host "  Rollback deployed but health check unreachable" -ForegroundColor Yellow
            $DeployReport.RollbackStatus = "PARTIAL — health check unreachable"
        }
    } else {
        Write-Host "  No backup found — manual rollback required" -ForegroundColor Red
        $DeployReport.RollbackStatus = "FAILED — no backup available"
    }

    $DeployReport.Verdict = "ROLLED BACK"
    $DeployReport.CompletedAt = (Get-Date -Format "yyyy-MM-dd HH:mm:ss K")

    # Generate rollback report
    $reportContent -replace "SUCCESS", "ROLLED BACK" | Set-Content $ReportPath -Encoding UTF8
}
