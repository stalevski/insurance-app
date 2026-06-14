#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Stops the locally-running Insurance Integration API.

.DESCRIPTION
    Finds the process listening on the app's HTTP port (default 5000) and stops it,
    plus its parent `dotnet run` host when present, so the dev session exits cleanly.
    Safe to run when nothing is listening.

.PARAMETER Port
    The HTTP port the app listens on. Defaults to 5000 (the "http" launch profile).

.EXAMPLE
    .\scripts\stop.ps1
.EXAMPLE
    .\scripts\stop.ps1 -Port 5001
#>
[CmdletBinding()]
param(
    [int]$Port = 5000
)

$ErrorActionPreference = 'Stop'

$listeners = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
if (-not $listeners) {
    Write-Host "No process is listening on port $Port. Nothing to stop." -ForegroundColor Yellow
    return
}

foreach ($processId in ($listeners.OwningProcess | Sort-Object -Unique)) {
    $proc = Get-Process -Id $processId -ErrorAction SilentlyContinue
    if (-not $proc) { continue }

    # If `dotnet run` launched the listener, the parent is the run host; stop it too
    # so the build/watch wrapper exits cleanly. Only ever stop a `dotnet` parent, never
    # the shell or editor that started it.
    $parentId = (Get-CimInstance Win32_Process -Filter "ProcessId=$processId" -ErrorAction SilentlyContinue).ParentProcessId
    $parent = if ($parentId) { Get-Process -Id $parentId -ErrorAction SilentlyContinue } else { $null }

    Write-Host "Stopping $($proc.ProcessName) (PID $processId) on port $Port..." -ForegroundColor Cyan
    Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue

    if ($parent -and $parent.ProcessName -eq 'dotnet') {
        Write-Host "Stopping parent dotnet host (PID $($parent.Id))..." -ForegroundColor Cyan
        Stop-Process -Id $parent.Id -Force -ErrorAction SilentlyContinue
    }
}

Start-Sleep -Milliseconds 500
if (Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue) {
    Write-Warning "Port $Port still has a listener; you may need to stop it manually."
} else {
    Write-Host "App stopped. Port $Port is free." -ForegroundColor Green
}
