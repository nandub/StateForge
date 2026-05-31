<#
.SYNOPSIS
Runs StateForge local benchmarks.

.DESCRIPTION
Runs create, read, update, concurrent read, concurrent update, enumeration, and cleanup benchmarks against StateForge.FileStore.

.PARAMETER RootPath
Benchmark root path.

.PARAMETER Sessions
Number of sessions. Defaults to 1000.

.PARAMETER PayloadBytes
Payload size in bytes. Defaults to 1024.

.PARAMETER Threads
Number of worker threads for concurrent tests.

.PARAMETER Compression
Enable compression.

.PARAMETER Encryption
Enable DPAPI encryption.

.PARAMETER Keep
Keep benchmark files.

.PARAMETER KeepBackups
Enable backup file creation during benchmark updates. Disabled by default for performance measurement.

.EXAMPLE
.\scripts\Invoke-StateForgeBenchmark.ps1 -Sessions 1000 -PayloadBytes 1024

.EXAMPLE
.\scripts\Invoke-StateForgeBenchmark.ps1 -RootPath ..\StateForgeBench -Sessions 10000 -PayloadBytes 4096 -Threads 8 -Compression -Keep

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
    [string]$RootPath,

    [Parameter()]
    [int]$Sessions = 1000,

    [Parameter()]
    [int]$PayloadBytes = 1024,

    [Parameter()]
    [int]$Threads = 0,

    [Parameter()]
    [switch]$Compression,

    [Parameter()]
    [switch]$Encryption,

    [Parameter()]
    [switch]$Aes,

    [Parameter()]
    [string]$AesKeyBase64,

    [Parameter()]
    [switch]$Keep,

    [Parameter()]
    [switch]$KeepBackups
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    $repoRoot = Split-Path -Parent $scriptRoot
    $projectPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Benchmarks\StateForge.Benchmarks.csproj'

    $arguments = @(
        'run',
        '--project',
        $projectPath,
        '--configuration',
        'Release',
        '--',
        '--sessions',
        $Sessions,
        '--payload-bytes',
        $PayloadBytes
    )

    if (-not [string]::IsNullOrWhiteSpace($RootPath)) {
        $arguments += '--root'
        $arguments += $RootPath
    }

    if ($Threads -gt 0) {
        $arguments += '--threads'
        $arguments += $Threads
    }

    if ($Compression.IsPresent) {
        $arguments += '--compression'
    }

    if ($Encryption.IsPresent) {
        $arguments += '--encryption'
    }

    if ($Aes.IsPresent) {
        $arguments += '--aes'

        if (-not [string]::IsNullOrWhiteSpace($AesKeyBase64)) {
            $arguments += '--aes-key'
            $arguments += $AesKeyBase64
        }
    }

    if ($Keep.IsPresent) {
        $arguments += '--keep'
    }

    if ($KeepBackups.IsPresent) {
        $arguments += '--keep-backups'
    }

    & dotnet @arguments

    if ($LASTEXITCODE -ne 0) {
        throw "StateForge benchmark failed with exit code $LASTEXITCODE."
    }

    [PSCustomObject]@{
        Project      = $projectPath
        Sessions     = $Sessions
        PayloadBytes = $PayloadBytes
        Success      = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
