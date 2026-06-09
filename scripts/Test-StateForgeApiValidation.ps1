<#
.SYNOPSIS
Runs StateForge public API validation tests.

.DESCRIPTION
Compiles and executes API validation tests that verify the expected public API shape for FileStore, Core, Prometheus, Security, and Telemetry references.

.EXAMPLE
.\scripts\Test-StateForgeApiValidation.ps1

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
    $projectPath = Join-Path -Path (Get-Location) -ChildPath 'src\StateForge.ApiValidationTests\StateForge.ApiValidationTests.csproj'

    & dotnet run --project $projectPath --configuration Release

    if ($LASTEXITCODE -ne 0) {
        throw "StateForge API validation tests failed with exit code $LASTEXITCODE."
    }

    [PSCustomObject]@{
        Project = $projectPath
        Success = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
