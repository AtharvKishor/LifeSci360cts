# Starts the services needed for the Reports page, each on its expected port,
# then launches the Angular frontend. Run from the repo root:  .\run-all.ps1
# Stops everything cleanly with Ctrl+C in this window (frontend), or close the
# spawned service windows.

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

function Ensure-PortFree([int]$port, [string]$name) {
    $conn = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
    if ($conn) {
        $procId = ($conn.OwningProcess | Select-Object -First 1)
        $proc = Get-Process -Id $procId -ErrorAction SilentlyContinue
        Write-Host "Port $port ($name) already in use by PID $procId ($($proc.ProcessName)) — leaving it running." -ForegroundColor Yellow
        return $false   # already running, don't start another
    }
    return $true        # free, safe to start
}

function Start-Service-Window([string]$name, [string]$dir, [int]$port) {
    if (Ensure-PortFree $port $name) {
        Write-Host "Starting $name on http://localhost:$port ..." -ForegroundColor Cyan
        Start-Process powershell -ArgumentList @(
            '-NoExit', '-Command',
            "Set-Location '$dir'; dotnet run --no-launch-profile --urls 'http://localhost:$port'"
        )
    }
}

# 1) Backend services (each in its own window)
Start-Service-Window 'AuthService'         (Join-Path $root 'AuthService')         5015
Start-Service-Window 'ReportingService'    (Join-Path $root 'ReportingService')    5278
Start-Service-Window 'NotificationService' (Join-Path $root 'NotificationService') 5103

# 2) Give the services a moment to spin up
Write-Host "Waiting 8s for services to start..." -ForegroundColor DarkGray
Start-Sleep -Seconds 8

# 3) Frontend (runs in THIS window; Ctrl+C stops it)
Write-Host "Starting Angular frontend on http://localhost:53719 ..." -ForegroundColor Cyan
Set-Location (Join-Path $root 'Frontend')
npm start
