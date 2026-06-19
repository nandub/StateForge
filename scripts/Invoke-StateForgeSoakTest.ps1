<#
.SYNOPSIS
Runs a configurable StateForge soak workload.

.DESCRIPTION
Runs StateForge.ScaleTests in soak mode. The workload repeatedly creates or updates sessions,
reads and verifies payloads, refreshes TTLs, performs lock/update cycles, and can run cleanup,
replication, and snapshot operations at configured intervals.

.PARAMETER RootPath
Root path for the soak test store.

.PARAMETER Sessions
Number of stable session keys in the workload.

.PARAMETER PayloadBytes
Payload size per session.

.PARAMETER Threads
Number of worker threads.

.PARAMETER DurationSeconds
Maximum runtime in seconds.

.PARAMETER MaxOperations
Maximum operation attempts before the run stops.

.PARAMETER CleanupInterval
Runs cleanup every N operation indexes. Use 0 to disable.

.PARAMETER ReplicationInterval
Runs full replication every N operation indexes. Use 0 to disable.

.PARAMETER SnapshotInterval
Runs a full snapshot every N operation indexes. Use 0 to disable.

.PARAMETER FinalReplication
Runs one final replication after the concurrent workload stops.

.PARAMETER FinalSnapshot
Runs one final snapshot after the concurrent workload stops.

.PARAMETER MaxErrors
Allowed workload errors before the run fails.

.PARAMETER OutputPath
Directory for JSON and CSV reports.

.PARAMETER Keep
Keeps the generated test store.

.EXAMPLE
.\scripts\Invoke-StateForgeSoakTest.ps1 -DurationSeconds 3600 -MaxOperations 100000

.NOTES
Compatible with Windows PowerShell 5.1.
#>
[CmdletBinding()]
param(
    [Parameter()]
    [string]$RootPath,

    [Parameter()]
    [ValidateRange(1, 1000000)]
    [int]$Sessions = 500,

    [Parameter()]
    [ValidateRange(1, 1048576)]
    [int]$PayloadBytes = 1024,

    [Parameter()]
    [ValidateRange(1, 256)]
    [int]$Threads = 4,

    [Parameter()]
    [ValidateRange(1, 604800)]
    [int]$DurationSeconds = 300,

    [Parameter()]
    [ValidateRange(1, 2147483647)]
    [int]$MaxOperations = 20000,

    [Parameter()]
    [ValidateRange(0, 2147483647)]
    [int]$CleanupInterval = 1000,

    [Parameter()]
    [ValidateRange(0, 2147483647)]
    [int]$ReplicationInterval = 0,

    [Parameter()]
    [ValidateRange(0, 2147483647)]
    [int]$SnapshotInterval = 0,

    [Parameter()]
    [switch]$FinalReplication,

    [Parameter()]
    [switch]$FinalSnapshot,

    [Parameter()]
    [ValidateRange(0, 2147483647)]
    [int]$MaxErrors = 0,

    [Parameter()]
    [string]$OutputPath = '.\artifacts\soak',

    [Parameter()]
    [switch]$Keep
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $repoRoot = Split-Path -Path $PSScriptRoot -Parent
    . (Join-Path -Path $PSScriptRoot -ChildPath 'StateForgePathDisplay.ps1')
    $projectPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.ScaleTests\StateForge.ScaleTests.csproj'
    $outputRoot = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)

    if (-not (Test-Path -LiteralPath $outputRoot)) {
        New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
    }

    $jsonPath = Join-Path -Path $outputRoot -ChildPath 'soak.json'
    $csvPath = Join-Path -Path $outputRoot -ChildPath 'soak.csv'

    $arguments = @(
        'run',
        '--project',
        $projectPath,
        '--configuration',
        'Release',
        '--',
        '--mode',
        'soak',
        '--sessions',
        [string]$Sessions,
        '--payload-bytes',
        [string]$PayloadBytes,
        '--threads',
        [string]$Threads,
        '--duration-seconds',
        [string]$DurationSeconds,
        '--max-operations',
        [string]$MaxOperations,
        '--cleanup-interval',
        [string]$CleanupInterval,
        '--replication-interval',
        [string]$ReplicationInterval,
        '--snapshot-interval',
        [string]$SnapshotInterval,
        '--max-errors',
        [string]$MaxErrors,
        '--export-json',
        $jsonPath,
        '--export-csv',
        $csvPath
    )

    if (-not [string]::IsNullOrWhiteSpace($RootPath)) {
        $arguments += '--root'
        $arguments += $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($RootPath)
    }

    if ($Keep.IsPresent) {
        $arguments += '--keep'
    }

    if ($FinalReplication.IsPresent) {
        $arguments += '--final-replication'
    }

    if ($FinalSnapshot.IsPresent) {
        $arguments += '--final-snapshot'
    }

    Push-Location -LiteralPath $repoRoot
    try {
        & dotnet @arguments
    }
    finally {
        Pop-Location
    }

    if ($LASTEXITCODE -ne 0) {
        throw "StateForge soak test failed with exit code $LASTEXITCODE."
    }

    if (-not (Test-Path -LiteralPath $jsonPath) -or -not (Test-Path -LiteralPath $csvPath)) {
        throw 'StateForge soak test did not write expected JSON and CSV reports.'
    }

    [PSCustomObject]@{
        Project         = ConvertTo-StateForgeDisplayPath -Path $projectPath -RepositoryRoot $repoRoot
        Sessions        = $Sessions
        PayloadBytes    = $PayloadBytes
        Threads         = $Threads
        DurationSeconds = $DurationSeconds
        MaxOperations   = $MaxOperations
        JsonPath        = ConvertTo-StateForgeDisplayPath -Path $jsonPath -RepositoryRoot $repoRoot
        CsvPath         = ConvertTo-StateForgeDisplayPath -Path $csvPath -RepositoryRoot $repoRoot
        Success         = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
