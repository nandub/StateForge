<#
.SYNOPSIS
Runs a StateForge replication test scenario.

.DESCRIPTION
Executes the replication foundation harness. This is a safe generated-store validation path for v0.21.0.

.EXAMPLE
.\scripts\Invoke-StateForgeReplication.ps1

.INPUTS
None.

.OUTPUTS
System.Management.Automation.PSCustomObject.

.NOTES
Compatible with Windows PowerShell 5.1.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    .\scripts\Test-StateForgeReplication.ps1

    [PSCustomObject]@{
        Success = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
