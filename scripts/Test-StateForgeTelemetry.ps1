<#
.SYNOPSIS
Tests StateForge telemetry counters against a running Kestrel harness.

.DESCRIPTION
Resets metrics, performs health/set/get/delete operations, and verifies
that read/write/delete counters increment.

.PARAMETER Url
Base URL for the running Kestrel harness.

.EXAMPLE
.\scripts\Test-StateForgeTelemetry.ps1 -Url http://localhost:5075

.INPUTS
None.

.OUTPUTS
System.Management.Automation.PSCustomObject.

.NOTES
Compatible with Windows PowerShell 5.1.
#>
[CmdletBinding()]
param(
    [Parameter()]
    [string]$Url = 'http://localhost:5075'
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $baseUrl = $Url.TrimEnd('/')

    Invoke-RestMethod -Method Post -Uri ($baseUrl + '/stateforge/metrics/reset') | Out-Null

    Invoke-RestMethod -Uri ($baseUrl + '/health') | Out-Null
    Invoke-RestMethod -Method Post -Uri ($baseUrl + '/session/telemetry/hello') | Out-Null
    Invoke-RestMethod -Uri ($baseUrl + '/session/telemetry') | Out-Null
    Invoke-RestMethod -Method Delete -Uri ($baseUrl + '/session/telemetry') | Out-Null

    $metrics = Invoke-RestMethod -Uri ($baseUrl + '/stateforge/metrics')

    $success = (($metrics.reads -gt 0) -and ($metrics.writes -gt 0) -and ($metrics.deletes -gt 0))

    [PSCustomObject]@{
        Url     = $Url
        Reads   = $metrics.reads
        Writes  = $metrics.writes
        Deletes = $metrics.deletes
        Success = $success
    }

    if (-not $success) {
        throw "Telemetry counters did not increment as expected."
    }
}
catch {
    Write-Error -ErrorRecord $_
}
