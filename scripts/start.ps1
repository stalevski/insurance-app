#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Runs the Insurance Integration API locally (Blazor UI + minimal API on http://localhost:5000).

.DESCRIPTION
    Starts the app from the repository root regardless of the current directory. Press Ctrl+C to
    stop it, or run `.\scripts\stop.ps1` from another terminal.

.PARAMETER Watch
    Use `dotnet watch` for hot reload during development.

.EXAMPLE
    .\scripts\start.ps1
.EXAMPLE
    .\scripts\start.ps1 -Watch
#>
[CmdletBinding()]
param(
    [switch]$Watch
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'src/InsuranceIntegration.Api'

Push-Location $repoRoot
try {
    if ($Watch) {
        dotnet watch --project $project run
    } else {
        dotnet run --project $project
    }
} finally {
    Pop-Location
}
