<#
.SYNOPSIS
Runs StateForge replica catch-up validation.

.DESCRIPTION
Validates replica catch-up dry-run planning, missing-file detection, changed-file detection,
extra-file detection, apply mode, and final convergence.

.EXAMPLE
.\scripts\Test-StateForgeReplicaCatchUp.ps1

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
    $projectPath = Join-Path -Path (Get-Location) -ChildPath 'src\StateForge.ReplicaCatchUpTests\StateForge.ReplicaCatchUpTests.csproj'

    & dotnet run --project $projectPath --configuration Release

    if ($LASTEXITCODE -ne 0) {
        throw "StateForge replica catch-up tests failed with exit code $LASTEXITCODE."
    }

    [PSCustomObject]@{
        Project = $projectPath
        Success = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
