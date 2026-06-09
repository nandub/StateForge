[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RootPath,

    [Parameter()]
    [string]$SnapshotPath = '.\artifacts\snapshots\stateforge-store-snapshot.json'
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $projectPath = Join-Path -Path (Get-Location) -ChildPath 'src\StateForge.PerformanceTests\StateForge.PerformanceTests.csproj'

    & dotnet run --project $projectPath --configuration Release

    if ($LASTEXITCODE -ne 0) {
        throw "StateForge performance snapshot validation failed with exit code $LASTEXITCODE."
    }

    [PSCustomObject]@{
        RootPath     = $RootPath
        SnapshotPath = $SnapshotPath
        Success      = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
