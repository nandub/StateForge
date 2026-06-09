<#
.SYNOPSIS
Runs StateForge replication manifest validation.

.DESCRIPTION
Validates manifest JSON writing and escaping for replication services.

.EXAMPLE
.\scripts\Test-StateForgeReplicationManifest.ps1

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
    $projectPath = Join-Path -Path (Get-Location) -ChildPath 'src\StateForge.ReplicationManifestTests\StateForge.ReplicationManifestTests.csproj'

    & dotnet run --project $projectPath --configuration Release

    if ($LASTEXITCODE -ne 0) {
        throw "StateForge replication manifest tests failed with exit code $LASTEXITCODE."
    }

    [PSCustomObject]@{
        Project = $projectPath
        Success = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
