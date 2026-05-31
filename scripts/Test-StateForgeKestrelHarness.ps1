<#
.SYNOPSIS
Tests a running StateForge Kestrel harness.

.DESCRIPTION
Uses HTTP to validate health, set, get, and delete operations.

.PARAMETER Url
Kestrel URL. Defaults to http://localhost:5075.

.EXAMPLE
.\scripts\Test-StateForgeKestrelHarness.ps1 -Url http://localhost:5075

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
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    $repoRoot = Split-Path -Parent $scriptRoot
    $projectPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.KestrelClientTest\StateForge.KestrelClientTest.csproj'

    & dotnet run --project $projectPath --configuration Release -- --url $Url

    if ($LASTEXITCODE -ne 0) {
        throw "StateForge Kestrel client test failed with exit code $LASTEXITCODE."
    }

    [PSCustomObject]@{
        Url     = $Url
        Success = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
