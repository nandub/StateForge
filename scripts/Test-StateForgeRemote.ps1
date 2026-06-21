<#
.SYNOPSIS
Runs StateForge remote gRPC/TLS integration tests.

.DESCRIPTION
Builds the remote client and host projects, then runs the remote test project. The
integration test starts StateForge.Remote.Host with a temporary TLS certificate and
exercises RemoteStateForgeStore over HTTPS/HTTP2.

.PARAMETER Configuration
Build configuration used for the test run.

.EXAMPLE
.\scripts\Test-StateForgeRemote.ps1

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
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [Parameter()]
    [switch]$IncludeIntegration
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    $repoRoot = Split-Path -Parent $scriptRoot
    $relativeProjectPath = 'tests\StateForge.Remote.Tests\StateForge.Remote.Tests.csproj'
    $projectPath = Join-Path -Path $repoRoot -ChildPath $relativeProjectPath

    if (-not (Test-Path -LiteralPath $projectPath)) {
        throw "Missing remote test project: $projectPath"
    }

    Push-Location -LiteralPath $repoRoot
    try {
        $testArguments = @('test', $relativeProjectPath, '--configuration', $Configuration)
        if (-not $IncludeIntegration) {
            $testArguments += @('--filter', 'TestCategory!=Integration')
        }

        $testProcess = Start-Process `
            -FilePath 'dotnet' `
            -ArgumentList $testArguments `
            -Wait `
            -PassThru `
            -NoNewWindow

        if ($testProcess.ExitCode -ne 0) {
            throw "Remote tests failed with exit code $($testProcess.ExitCode)."
        }
    }
    finally {
        Pop-Location
    }

    . (Join-Path -Path $scriptRoot -ChildPath 'StateForgePathDisplay.ps1')

    [PSCustomObject]@{
        Project       = ConvertTo-StateForgeDisplayPath -Path $projectPath -RepositoryRoot $repoRoot
        Configuration = $Configuration
        Integration   = [bool]$IncludeIntegration
        Success       = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
