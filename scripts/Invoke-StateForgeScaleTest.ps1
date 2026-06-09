<#
.SYNOPSIS
Runs StateForge scale tests.

.DESCRIPTION
Runs the StateForge.ScaleTests project with configurable session count, payload size, and thread count.

.PARAMETER RootPath
Root path for the scale test store.

.PARAMETER Sessions
Number of sessions to create.

.PARAMETER PayloadBytes
Payload size per session.

.PARAMETER Threads
Number of worker threads.

.PARAMETER Keep
Keeps the generated test store.

.EXAMPLE
.\scripts\Invoke-StateForgeScaleTest.ps1 -Sessions 25000 -PayloadBytes 1024 -Threads 8

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
    [int]$Sessions = 25000,

    [Parameter()]
    [int]$PayloadBytes = 1024,

    [Parameter()]
    [int]$Threads = 8,

    [Parameter()]
    [switch]$Keep
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $projectPath = Join-Path -Path (Get-Location) -ChildPath 'src\StateForge.ScaleTests\StateForge.ScaleTests.csproj'
    $arguments = @('run', '--project', $projectPath, '--configuration', 'Release', '--')

    if (-not [string]::IsNullOrWhiteSpace($RootPath)) {
        $arguments += '--root'
        $arguments += $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($RootPath)
    }

    $arguments += '--sessions'
    $arguments += [string]$Sessions
    $arguments += '--payload-bytes'
    $arguments += [string]$PayloadBytes
    $arguments += '--threads'
    $arguments += [string]$Threads

    if ($Keep.IsPresent) {
        $arguments += '--keep'
    }

    & dotnet @arguments

    if ($LASTEXITCODE -ne 0) {
        throw "StateForge scale tests failed with exit code $LASTEXITCODE."
    }

    [PSCustomObject]@{
        Project      = $projectPath
        Sessions     = $Sessions
        PayloadBytes = $PayloadBytes
        Threads      = $Threads
        Success      = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
