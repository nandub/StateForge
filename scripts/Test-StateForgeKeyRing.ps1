<#
.SYNOPSIS
Tests StateForge AES key-ring creation, validation, and rotation.

.DESCRIPTION
Creates a temporary key ring, validates it, rotates it, and validates it again.

.PARAMETER OutFile
Optional output key-ring path.

.EXAMPLE
.\scripts\Test-StateForgeKeyRing.ps1 -OutFile .\stateforge-keyring-test.json

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
    [string]$OutFile = '.\stateforge-keyring-test.json'
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    $repoRoot = Split-Path -Parent $scriptRoot
    $toolProject = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Tools\StateForge.Tools.csproj'
    $resolvedOutFile = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutFile)

    if (Test-Path -LiteralPath $resolvedOutFile) {
        Remove-Item -LiteralPath $resolvedOutFile -Force
    }

    & dotnet run --project $toolProject -- keyring-create --out $resolvedOutFile --key-id key-001
    if ($LASTEXITCODE -ne 0) { throw "keyring-create failed." }

    & dotnet run --project $toolProject -- keyring-validate --ring $resolvedOutFile
    if ($LASTEXITCODE -ne 0) { throw "keyring-validate failed after create." }

    & dotnet run --project $toolProject -- keyring-rotate --ring $resolvedOutFile --new-key-id key-002
    if ($LASTEXITCODE -ne 0) { throw "keyring-rotate failed." }

    & dotnet run --project $toolProject -- keyring-validate --ring $resolvedOutFile
    if ($LASTEXITCODE -ne 0) { throw "keyring-validate failed after rotate." }

    [PSCustomObject]@{
        OutFile = $resolvedOutFile
        Success = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
