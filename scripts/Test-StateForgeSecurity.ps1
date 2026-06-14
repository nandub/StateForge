<#
.SYNOPSIS
Runs StateForge v1 security validation.

.DESCRIPTION
Validates authenticated AES records, tamper and wrong-key rejection, legacy AES compatibility,
and validated atomic key-ring persistence.

.NOTES
Compatible with Windows PowerShell 5.1.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $repoRoot = Split-Path -Parent $PSScriptRoot
    $projectPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.SecurityTests\StateForge.SecurityTests.csproj'

    & dotnet run --project $projectPath --configuration Release
    if ($LASTEXITCODE -ne 0) {
        throw "StateForge security tests failed with exit code $LASTEXITCODE."
    }

    [PSCustomObject]@{
        Project = $projectPath
        Success = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
